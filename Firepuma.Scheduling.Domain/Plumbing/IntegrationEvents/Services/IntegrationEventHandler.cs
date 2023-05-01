using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.EventMediation.IntegrationEvents.Helpers;
using Firepuma.EventMediation.IntegrationEvents.ValueObjects;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Entities;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.Reflection;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;

/// <summary>
/// This class is responsible for handling integration events by using the
/// <see cref="IntegrationEventEnvelope.EventType"/> and finding a class with an
/// attribute containing the same type. IntegrationEvent handlers should implement the
/// <see cref="Abstractions.ICommandsFactory{TEvent}"/> which returns a list of new Commands to
/// execute. This class also executes those commands in parallel, records
/// metrics and logs useful info.
/// Local events should have the <see cref="Attributes.IncomingIntegrationEventTypeAttribute"/>.
/// </summary>
public class IntegrationEventHandler : IIntegrationEventHandler
{
    private readonly ILogger<IntegrationEventHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IIntegrationEventExecutionRepository _integrationEventExecutionRepository;

    public IntegrationEventHandler(
        ILogger<IntegrationEventHandler> logger,
        IMediator mediator,
        IIntegrationEventExecutionRepository integrationEventExecutionRepository)
    {
        _logger = logger;
        _mediator = mediator;
        _integrationEventExecutionRepository = integrationEventExecutionRepository;
    }

    public async Task<bool> TryHandleEventAsync(IntegrationEventEnvelope integrationEventEnvelope, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryDeserializeIntegrationEvent(integrationEventEnvelope, out var eventPayload))
            {
                _logger.LogWarning(
                    "Unable to deserialize integration event with type {Type} and id {Id}",
                    integrationEventEnvelope.EventType, integrationEventEnvelope.EventId);
                return false;
            }

            string? sourceCommandId = null;
            if (eventPayload is BaseOutgoingIntegrationEvent baseOutgoingIntegrationEvent)
            {
                sourceCommandId = baseOutgoingIntegrationEvent.CommandId;
            }

            var executionEvent = new IntegrationEventExecution
            {
                Id = IntegrationEventExecution.GenerateId(),
                EventId = integrationEventEnvelope.EventId,
                Status = IntegrationEventExecution.ExecutionStatus.New,
                StartedOn = DateTime.UtcNow,
                TypeName = IntegrationEventExecution.GetTypeName(eventPayload.GetType()),
                TypeNamespace = IntegrationEventExecution.GetTypeNamespace(eventPayload.GetType()),
                Payload = IntegrationEventExecution.GetSerializedPayload(eventPayload),
                SourceCommandId = sourceCommandId,
            };

            executionEvent = await _integrationEventExecutionRepository.AddItemAsync(
                executionEvent,
                cancellationToken);

            var createCommandsRequest = CreateCommandsRequest(
                integrationEventEnvelope,
                eventPayload);

            var commandCreationStopwatch = Stopwatch.StartNew();
            // this mediator Send will be handled by the appropriate ICommandsFactory<> implementation / handler
            var commands = (await _mediator.Send(createCommandsRequest, cancellationToken)).ToArray();
            commandCreationStopwatch.Stop();

            if (commands.Length == 0)
            {
                _logger.LogInformation(
                    "No commands were produced for integration event type {EventType} id {IntegrationEventId}, creation " +
                    "attempt took {DurationMs} ms. The reason for no commands could be because it returns 0 commands in specific " +
                    "conditions, see previous logs messages for potential reason",
                    eventPayload.GetType().FullName, integrationEventEnvelope.EventId, commandCreationStopwatch.ElapsedMilliseconds);

                executionEvent = PopulateExecutionEvent(
                    executionEvent,
                    integrationEventEnvelope,
                    TimeSpan.Zero,
                    0,
                    Array.Empty<ICommandRequest>(),
                    new ConcurrentBag<IntegrationEventExecution.ExecutionError>(),
                    eventPayload,
                    out _);

                await _integrationEventExecutionRepository.ReplaceItemAsync(
                    executionEvent,
                    cancellationToken);

                return true;
            }

            _logger.LogDebug(
                "Creation (not yet execution) of {Count} commands took {DurationMs} ms",
                commands.Length, commandCreationStopwatch.ElapsedMilliseconds);

            var executionStopwatch = Stopwatch.StartNew();

            var commandErrors = new ConcurrentBag<IntegrationEventExecution.ExecutionError>();
            var successCount = await ExecuteCommandsAsync(
                integrationEventEnvelope,
                commands,
                eventPayload,
                commandErrors,
                cancellationToken);

            executionStopwatch.Stop();

            if (successCount > 0)
            {
                _logger.LogInformation(
                    "Successfully executed {Count}/{Total} commands in {Milliseconds} ms caused by integration event type {EventType} and id {IntegrationEventId}",
                    successCount, commands.Length, executionStopwatch.ElapsedMilliseconds, eventPayload.GetType().FullName, integrationEventEnvelope.EventId);
            }

            executionEvent = PopulateExecutionEvent(
                executionEvent,
                integrationEventEnvelope,
                executionStopwatch.Elapsed,
                successCount,
                commands,
                commandErrors,
                eventPayload,
                out var considerEventHandled);

            await _integrationEventExecutionRepository.ReplaceItemAsync(
                executionEvent,
                cancellationToken);

            return considerEventHandled;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to try and handle integration event, event Id: {Id}, Type: {Type}, Payload: {Payload}",
                integrationEventEnvelope.EventId, integrationEventEnvelope.EventType, integrationEventEnvelope.EventPayload);
            throw;
        }
    }

    private async Task<int> ExecuteCommandsAsync(
        IntegrationEventEnvelope integrationEventEnvelope,
        IEnumerable<ICommandRequest> commands,
        object eventPayload,
        ConcurrentBag<IntegrationEventExecution.ExecutionError> errors,
        CancellationToken cancellationToken)
    {
        var successCount = 0;

        await Task.WhenAll(commands.Select(
            async command =>
            {
                try
                {
                    await _mediator.Send(command, cancellationToken);
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to execute command {Type} with id {CommandId}, from integration event type {EventType} and id {IntegrationEventId}",
                        command.GetType().FullName, command.CommandId, eventPayload.GetType().FullName, integrationEventEnvelope.EventId);

                    errors.Add(new IntegrationEventExecution.ExecutionError
                    {
                        ErrorMessage = exception.Message,
                        ErrorStackTrack = exception.StackTrace,
                    });
                }
            }));

        return successCount;
    }

    private IntegrationEventExecution PopulateExecutionEvent(
        IntegrationEventExecution executionEvent,
        IntegrationEventEnvelope integrationEventEnvelope,
        TimeSpan executionDuration,
        int successCount,
        IReadOnlyCollection<ICommandRequest> commands,
        ConcurrentBag<IntegrationEventExecution.ExecutionError> errors,
        object eventPayload,
        out bool considerEventHandled)
    {
        executionEvent.CompletedOn = DateTime.UtcNow;
        executionEvent.DurationInSeconds = executionDuration.TotalSeconds;

        var allCommandIds = commands.Select(c => c.CommandId).ToArray();

        const int maxCommandIds = 500;
        if (commands.Count > maxCommandIds)
        {
            // Storing too many strings in an array of a mongo document will bloat it a lot, so rather cap it
            _logger.LogError("There were a total of {Count} command ids but will only store {Max} on " +
                             "the IntegrationExecutionEvent, event type {EventType}, id {IntegrationEventId}, " +
                             "all command Ids: {CommandIds}",
                commands.Count, maxCommandIds, eventPayload.GetType().FullName, integrationEventEnvelope.EventId,
                string.Join(", ", commands.Select(c => c.CommandId)));
        }

        executionEvent.CommandIds = allCommandIds.Take(maxCommandIds).ToArray();

        executionEvent.SuccessfulCommandCount = successCount;
        executionEvent.TotalCommandCount = commands.Count;
        executionEvent.ErrorCount = errors.Count;

        executionEvent.Status = successCount == commands.Count
            ? IntegrationEventExecution.ExecutionStatus.Successful
            : (successCount > 0
                ? IntegrationEventExecution.ExecutionStatus.PartiallySuccessful
                : IntegrationEventExecution.ExecutionStatus.Failed);

        if (errors.Any())
        {
            executionEvent.Errors = errors.ToArray();
        }

        // The reason for considering PartiallySuccessful as "handled" is because we don't
        // want other commands to be executed twice if we allow a full retry. We will rather
        // abort if it is partially successful and rely on human intervention.
        considerEventHandled = executionEvent.Status is
            IntegrationEventExecution.ExecutionStatus.Successful
            or IntegrationEventExecution.ExecutionStatus.PartiallySuccessful;

        if (executionEvent.Status == IntegrationEventExecution.ExecutionStatus.PartiallySuccessful)
        {
            _logger.LogError(
                "Partially successful integration event will not be retried, type {EventType}, id {IntegrationEventId}",
                eventPayload.GetType().FullName, integrationEventEnvelope.EventId);
        }

        if (!considerEventHandled)
        {
            var timeString = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            // Suffix the EventId to ensure the next attempt to handle the integration event does
            // not fail when it tries to insert the same EventId again
            executionEvent.EventId += $"-status-{executionEvent.Status.ToString()}-{timeString}";
        }

        return executionEvent;
    }

    private bool TryDeserializeIntegrationEvent(
        IntegrationEventEnvelope envelope,
        [NotNullWhen(true)] out object? eventPayload)
    {
        if (envelope.TryDeserializeIntegrationEventWithAttribute<IncomingIntegrationEventTypeAttribute>(
                _logger,
                ReflectionHelpers.GetAllLoadableTypes(),
                attribute => attribute.IntegrationEventType,
                out var localEventPayload))
        {
            eventPayload = localEventPayload;
            return true;
        }

        eventPayload = null;
        return false;
    }

    private static IRequest<IEnumerable<ICommandRequest>> CreateCommandsRequest(
        IntegrationEventEnvelope eventEnvelope,
        object eventPayload)
    {
        var type = eventPayload.GetType();
        var createCommandsRequest = (IRequest<IEnumerable<ICommandRequest>>)
            (Activator.CreateInstance(
                 typeof(CreateCommandsFromIntegrationEventRequest<>).MakeGenericType(type),
                 args: new[] { eventEnvelope, eventPayload })
             ?? throw new InvalidOperationException($"Could not create wrapper type for {type}"));
        return createCommandsRequest;
    }
}