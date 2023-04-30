using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Firepuma.BusMessaging.Abstractions.Config;
using Firepuma.BusMessaging.Abstractions.Services;
using Firepuma.DatabaseRepositories.MongoDb.Abstractions.Indexes;
using Firepuma.EventMediation.IntegrationEvents.Constants;
using Firepuma.EventMediation.IntegrationEvents.ValueObjects;
using Firepuma.Scheduling.Domain.Commands;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;
using Firepuma.Scheduling.Domain.QuerySpecifications;
using Firepuma.Scheduling.Domain.Repositories;
using Firepuma.Scheduling.Domain.ScheduledTasks.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Firepuma.Scheduling.Worker.PubSubHandling;

[ApiController]
[Route("api/[controller]")]
public class PubSubListenerController : ControllerBase
{
    private readonly ILogger<PubSubListenerController> _logger;
    private readonly IBusMessageParser _busMessageParser;
    private readonly IIntegrationEventHandler _integrationEventHandler;
    private readonly IMongoIndexesApplier _mongoIndexesApplier;
    private readonly IMediator _mediator;
    private readonly IScheduledTaskRepository _scheduledTaskRepository;

    public PubSubListenerController(
        ILogger<PubSubListenerController> logger,
        IBusMessageParser busMessageParser,
        IIntegrationEventHandler integrationEventHandler,
        IMongoIndexesApplier mongoIndexesApplier,
        IMediator mediator,
        IScheduledTaskRepository scheduledTaskRepository)
    {
        _logger = logger;
        _busMessageParser = busMessageParser;
        _integrationEventHandler = integrationEventHandler;
        _mongoIndexesApplier = mongoIndexesApplier;
        _mediator = mediator;
        _scheduledTaskRepository = scheduledTaskRepository;
    }

    [HttpPost]
    public async Task<IActionResult> HandleBusMessageAsync(
        JsonDocument requestBody,
        CancellationToken cancellationToken)
    {
        if (!_busMessageParser.TryParseMessage(requestBody, out var parsedMessageEnvelope, out var parseFailureReason))
        {
            if (requestBody.RootElement.TryGetProperty("message", out var pubSubMessage)
                && pubSubMessage.TryGetProperty("data", out var messageData))
            {
                var base64DataString = messageData.GetString();

                if (base64DataString == null
                    || !TryDecodeBase64String(base64DataString, out var messageDataString))
                {
                    messageDataString = base64DataString;
                }

                if (messageDataString != null && messageDataString.StartsWith("ScheduledJob:"))
                {
                    var jobName = messageDataString.Substring("ScheduledJob:".Length);

                    _logger.LogInformation(
                        "Detected a ScheduledJob with job name '{JobName}', will not start its execution",
                        jobName);

                    if (jobName == "notify-due-tasks")
                    {
                        await NotifyDueTasksAsync(cancellationToken);
                    }
                    else
                    {
                        var handleCommand = new HandleScheduledJob
                        {
                            ScheduledJobName = jobName,
                        };
                        await _mediator.Send(handleCommand, cancellationToken);
                    }

                    return Ok($"Handled ScheduledJob with name {jobName}");
                }

                if (DataContainsGithubWorkflowEventName(messageDataString ?? "{}", out var githubWorkflowEventName)
                    && string.Equals(githubWorkflowEventName, "NewRevisionDeployed", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Detected a GithubWorkflowEventName message for 'NewRevisionDeployed', now running once-off logic after new deployments");

                    await _mongoIndexesApplier.ApplyAllIndexes(cancellationToken);

                    return Ok("Detected a GithubWorkflowEventName message for 'NewRevisionDeployed', ran once-off logic after new deployments");
                }
            }

            _logger.LogError("Failed to parse message, parseFailureReason: {ParseFailureReason}", parseFailureReason);
            _logger.LogDebug("Message that failed to parse had body: {Body}", JsonSerializer.Serialize(requestBody));
            return BadRequest(parseFailureReason);
        }

        if (!parsedMessageEnvelope.Attributes.TryGetValue(FirepumaAttributeKeys.MESSAGE_TYPE, out var messageType))
        {
            var attributeKeysCombined = string.Join(", ", parsedMessageEnvelope.Attributes.Keys);
            _logger.LogError(
                "Failed to extract message because type is not in attributes, expected " +
                "attribute {MissingAttribute} but the only attributes are {Attributes}",
                FirepumaAttributeKeys.MESSAGE_TYPE, attributeKeysCombined);
            return BadRequest($"Failed to extract message because type is not in attributes, expected " +
                              $"attribute {FirepumaAttributeKeys.MESSAGE_TYPE} but the only attributes are {attributeKeysCombined}");
        }

        _logger.LogDebug(
            "Parsed message: id {Id}, type: {Type}, payload: {Payload}",
            parsedMessageEnvelope.MessageId, messageType, parsedMessageEnvelope.MessagePayload);

        var deserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };
        var messagePayload = JsonSerializer.Deserialize<JsonDocument>(parsedMessageEnvelope.MessagePayload ?? "{}", deserializeOptions);

        if (messagePayload == null)
        {
            _logger.LogError(
                "Parsed message deserialization resulted in NULL, message id {MessageId}, type {MessageType}",
                parsedMessageEnvelope.MessageId, messageType);

            return BadRequest("Parsed message deserialization resulted in NULL");
        }

        var integrationEventEnvelope =
            parsedMessageEnvelope.MessageId?.Contains(BusMessagingPubSubConstants.LOCAL_DEVELOPMENT_PARSED_MESSAGE_ID_SUFFIX) == true
                ? new IntegrationEventEnvelope // this version is typically used for local development
                {
                    EventId = parsedMessageEnvelope.MessageId,
                    EventType = messageType,
                    EventPayload = parsedMessageEnvelope.MessagePayload!,
                }
                : messagePayload.Deserialize<IntegrationEventEnvelope>();

        if (integrationEventEnvelope == null)
        {
            _logger.LogError(
                "IntegrationEventEnvelope deserialization resulted in a NULL, message id {MessageId}, type {MessageType}",
                parsedMessageEnvelope.MessageId, messageType);

            return BadRequest("IntegrationEventEnvelope deserialization resulted in a NULL");
        }

        var handled = await _integrationEventHandler.TryHandleEventAsync(integrationEventEnvelope, cancellationToken);
        if (!handled)
        {
            _logger.LogError(
                "Integration event was not handled for message id {MessageId}, event type {EventType}",
                parsedMessageEnvelope.MessageId, integrationEventEnvelope.EventType);
            return BadRequest("Integration event was not handled");
        }

        return Accepted(integrationEventEnvelope);
    }

    private async Task NotifyDueTasksAsync(CancellationToken cancellationToken)
    {
        var checkAheadDuration = TimeSpan.FromSeconds(55);
        var nowWithAddedBufferForProcessingTime = DateTime.UtcNow.Add(checkAheadDuration);
        var querySpecification = new ScheduledTaskQuerySpecifications.DueNow(nowWithAddedBufferForProcessingTime);

        var scheduledTasks = (await _scheduledTaskRepository.GetItemsAsync(querySpecification, cancellationToken)).ToList();

        if (scheduledTasks.Any())
        {
            _logger.LogInformation("Notifying applications of {Count} due tasks", scheduledTasks.Count);
        }

        var commands = scheduledTasks
            .Select(task => new NotifyDueTask
            {
                ScheduledTask = task,
            });

        var commandTasks = commands
            .Select(async command =>
            {
                try
                {
                    await _mediator.Send(command, cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to NotifyDueTask, ScheduledTaskId: {ScheduledTask} ApplicationId: {ApplicationId}, ExtraValues: {ExtraValues}",
                        command.ScheduledTask.Id, command.ScheduledTask.ApplicationId, JsonSerializer.Serialize(command.ScheduledTask.ExtraValues));
                }
            });

        await Task.WhenAll(commandTasks);
    }

    private static bool TryDecodeBase64String(
        string base64,
        [NotNullWhen(true)] out string? decodedString)
    {
        var buffer = new Span<byte>(new byte[base64.Length]);

        if (!Convert.TryFromBase64String(base64, buffer, out var bytesWritten))
        {
            decodedString = null;
            return false;
        }

        decodedString = System.Text.Encoding.UTF8.GetString(buffer.ToArray(), 0, bytesWritten);
        return true;
    }

    private static bool DataContainsGithubWorkflowEventName(
        string messageDataString,
        [NotNullWhen(true)] out string? githubWorkflowEventName)
    {
        if (messageDataString.Contains("GithubWorkflowEventName"))
        {
            try
            {
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() },
                };
                var container = JsonSerializer.Deserialize<GithubWorkflowEventContainer>(messageDataString, deserializeOptions);
                if (!string.IsNullOrWhiteSpace(container?.GithubWorkflowEventName))
                {
                    githubWorkflowEventName = container.GithubWorkflowEventName;
                    return true;
                }
            }
            catch (Exception)
            {
                githubWorkflowEventName = null;
                return false;
            }
        }

        githubWorkflowEventName = null;
        return false;
    }

    private class GithubWorkflowEventContainer
    {
        public string GithubWorkflowEventName { get; set; } = null!;
    }
}