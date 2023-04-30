using System.Diagnostics.CodeAnalysis;
using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.EventMediation.IntegrationEvents.Factories;
using Firepuma.EventMediation.IntegrationEvents.Helpers;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Entities;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Extensions;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Services;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Constants;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable ArgumentsStyleNamedExpression

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;

/// <summary>
/// This class is responsible for determining the Type of the integration event, creating
/// an <c>IntegrationEventEnvelope</c>, populating the <c>ExtraValues</c> of
/// the <c>CommandExecutionEvent</c> and sending the event via the <see cref="IIntegrationEventTransport"/>
/// It tries to determine the event type by checking the
/// attributes on the payload, like <see cref="OutgoingIntegrationEventTypeAttribute"/>.
/// </summary>
public class CommandEventPublisher : ICommandEventPublisher
{
    private readonly ILogger<CommandEventPublisher> _logger;
    private readonly ICommandContext _commandContext;
    private readonly ICommandExecutionRepository _commandExecutionRepository;
    private readonly IIntegrationEventTransport _integrationEventTransport;

    public CommandEventPublisher(
        ILogger<CommandEventPublisher> logger,
        ICommandContext commandContext,
        ICommandExecutionRepository commandExecutionRepository,
        IIntegrationEventTransport integrationEventTransport)
    {
        _logger = logger;
        _commandContext = commandContext;
        _commandExecutionRepository = commandExecutionRepository;
        _integrationEventTransport = integrationEventTransport;
    }

    public async Task PublishAsync(
        ICommandRequest commandRequest,
        object integrationEvent,
        ISendEventTarget target,
        CancellationToken cancellationToken)
    {
        if (!TryGetIntegrationEventType(integrationEvent, out var integrationEventType))
        {
            var eventPayload = JsonConvert.SerializeObject(integrationEvent, GetPublishResultSerializerSettings());
            _logger.LogError(
                "Unable to publish because no integration event type mapping for event object type {Type}, " +
                "command type {CommandType}, command id {CommandId}, intended event payload: '{Payload}'",
                integrationEvent.GetType().FullName, commandRequest.GetType().FullName, commandRequest.CommandId, eventPayload);
            return;
        }

        var envelope = IntegrationEventEnvelopeFactory.CreateEnvelope(integrationEventType, integrationEvent);

        if (!_commandContext.TryGetCommandExecutionEvent(commandRequest, out var commandExecutionEvent))
        {
            _logger.LogError(
                "Unable to get CommandExecutionEvent from context for Command Id {CommandId} and type {CommandType}, " +
                "cannot store pending integration event in its ExtraValues as fallback. " +
                "Integration event Id: {Id}, Type: {Type}, Payload: {Payload}",
                commandRequest.CommandId, commandRequest.GetType().FullName, envelope.EventId, envelope.EventType, envelope.EventPayload);
        }
        else
        {
            if (commandExecutionEvent.ExtraValues.TryGetValue(
                    IntegrationEventExtraValuesKeys.IntegrationEventPayloadType.ToString(),
                    out var previouslyAddedPayloadType))
            {
                commandExecutionEvent.ExtraValues.TryGetValue(IntegrationEventExtraValuesKeys.IntegrationEventId.ToString(),
                    out var previouslyAddedId);
                commandExecutionEvent.ExtraValues.TryGetValue(IntegrationEventExtraValuesKeys.IntegrationEventPayloadJson.ToString(),
                    out var previouslyAddedPayload);

                _logger.LogError(
                    "When publishing multiple integration events from a single command, only the most recent one will be " +
                    "stored on the commandExecutionEvent.ExtraValues. " +
                    "Previously added one will now be overwritten, it had Id: {Id}, Type: '{Type}', Payload: {Payload}. " +
                    "To create multiple integration events from a single command, consider publishing an event that generates multiple " +
                    "commands via its ICommandsFactory<> handler",
                    previouslyAddedId, previouslyAddedPayloadType, previouslyAddedPayload);
            }

            commandExecutionEvent.ExtraValues[IntegrationEventExtraValuesKeys.IntegrationEventId.ToString()] = envelope.EventId;
            commandExecutionEvent.ExtraValues[IntegrationEventExtraValuesKeys.IntegrationEventPayloadType.ToString()] = envelope.EventType;
            commandExecutionEvent.ExtraValues[IntegrationEventExtraValuesKeys.IntegrationEventPayloadJson.ToString()] = envelope.EventPayload;
        }

        Exception? publishException = null;
        bool publishedSuccessfully;
        try
        {
            var publishEventTask = _integrationEventTransport.SendAsync(envelope, target, cancellationToken);

            await Task.WhenAll(
                commandExecutionEvent != null
                    ? UpsertCommandExecutionEventLogAndSwallowException(commandRequest, commandExecutionEvent, cancellationToken)
                    : Task.CompletedTask,
                publishEventTask);

            publishedSuccessfully = true;
        }
        catch (Exception exception)
        {
            publishException = exception;
            publishedSuccessfully = false;
        }

        if (commandExecutionEvent != null)
        {
            commandExecutionEvent.ExtraValues[IntegrationEventExtraValuesKeys.IntegrationEventPublishResultTime.ToString()] = DateTime.UtcNow;
            commandExecutionEvent.ExtraValues[IntegrationEventExtraValuesKeys.IntegrationEventPublishResultSuccess.ToString()] = publishedSuccessfully;

            if (publishException != null)
            {
                _logger.LogError(
                    publishException,
                    "Error trying to publish integration event, will now store it on the CommandExecution.ExtraValues[{Property}], " +
                    "integration event Id: {Id}, Type: {Type}, Payload: {Payload}",
                    IntegrationEventExtraValuesKeys.IntegrationEventPublishResultError,
                    envelope.EventId, envelope.EventType, envelope.EventPayload);

                var publishResultError = JsonConvert.SerializeObject(new { publishException.Message, publishException.StackTrace }, GetPublishResultSerializerSettings());
                commandExecutionEvent.ExtraValues[IntegrationEventExtraValuesKeys.IntegrationEventPublishResultError.ToString()] = publishResultError;
            }

            // commandExecutionEvent.ExtraValues.Remove(IntegrationEventExtraValuesKeys.IntegrationEventLockUntilUnixSeconds.ToString());
        }
    }

    private static bool TryGetIntegrationEventType<TMessage>(
        TMessage messagePayload,
        [NotNullWhen(true)] out string? eventType)
    {
        if (EventTypeHelpers.TryGetEventTypeFromAttribute<OutgoingIntegrationEventTypeAttribute>(
                messagePayload,
                attribute => attribute.IntegrationEventType,
                out var localEventType))
        {
            eventType = localEventType;
            return true;
        }

        eventType = null;
        return false;
    }

    private async Task UpsertCommandExecutionEventLogAndSwallowException(
        ICommandRequest commandRequest,
        CommandExecutionMongoDbEvent commandExecutionEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            await _commandExecutionRepository.ReplaceItemAsync(
                commandExecutionEvent,
                cancellationToken);
        }
        catch (Exception exception)
        {
            var extraValues = JsonConvert.SerializeObject(commandExecutionEvent.ExtraValues);
            _logger.LogError(
                exception,
                "Unable to upsert CommandExecutionEvent for Command Id {Id} and " +
                "type {Type}, ExecutionEvent Id {ExecutionEventId} and execution ExtraValues: {ExecutionExtraValues}",
                commandRequest.CommandId, commandRequest.GetType().FullName, commandExecutionEvent.Id, extraValues);
        }
    }

    private static JsonSerializerSettings GetPublishResultSerializerSettings()
    {
        var jsonSerializerSettings = new JsonSerializerSettings();
        jsonSerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        return jsonSerializerSettings;
    }
}