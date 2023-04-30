using Firepuma.CommandsAndQueries.Abstractions.Commands;
using MediatR;

// ReSharper disable UnusedType.Global

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;

/// <summary>
/// IntegrationEvent handlers should implement this interface to handle it automatically. For an
/// example, refer to <see cref="Domain.IntegrationEvents.IncomingOnly.AddScheduledTaskRequest.CommandsFactory"/>.
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface ICommandsFactory<TEvent> : IRequestHandler<CreateCommandsFromIntegrationEventRequest<TEvent>, IEnumerable<ICommandRequest>>
{
}