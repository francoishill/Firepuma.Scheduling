using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;

public interface ICommandEventPublisher
{
    Task PublishAsync(
        ICommandRequest commandRequest,
        object integrationEvent,
        ISendEventTarget target,
        CancellationToken cancellationToken);
}