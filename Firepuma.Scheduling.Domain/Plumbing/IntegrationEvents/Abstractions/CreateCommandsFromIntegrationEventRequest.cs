using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.EventMediation.IntegrationEvents.ValueObjects;
using MediatR;

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;

/// <summary>
/// A wrapper class which gets created by <see cref="Services.IntegrationEventHandler"/> to add some context that
/// can be used by OutgoingIntegrationEvent handlers. For an example handler, refer
/// to <see cref="Domain.IntegrationEvents.IncomingOnly.AddScheduledTaskRequest.CommandsFactory"/>.
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public class CreateCommandsFromIntegrationEventRequest<TEvent> : IRequest<IEnumerable<ICommandRequest>>
{
    public IntegrationEventEnvelope EventEnvelope { get; init; }
    public TEvent EventPayload { get; init; }

    public CreateCommandsFromIntegrationEventRequest(
        IntegrationEventEnvelope eventEnvelope,
        TEvent eventPayload)
    {
        // This constructor is used with reflection from IntegrationEventHandler.CreateCommandsRequest 

        EventEnvelope = eventEnvelope;
        EventPayload = eventPayload;
    }
}