using Firepuma.EventMediation.IntegrationEvents.ValueObjects;

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;

public interface IIntegrationEventHandler
{
    Task<bool> TryHandleEventAsync(
        IntegrationEventEnvelope integrationEventEnvelope,
        CancellationToken cancellationToken);
}