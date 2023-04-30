using Firepuma.EventMediation.IntegrationEvents.ValueObjects;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;

public interface IIntegrationEventTransport
{
    Task SendAsync(
        IntegrationEventEnvelope eventEnvelope,
        ISendEventTarget target,
        CancellationToken cancellationToken);
}