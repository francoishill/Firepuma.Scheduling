using Firepuma.EventMediation.IntegrationEvents.Factories;

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;

/// <summary>
/// An abstract base class from which IntegrationEvent can inherit, this class simply adds
/// properties with sensible default values, like <see cref="CreatedOn"/> and <see cref="IntegrationEventId"/>.
/// </summary>
public abstract class BaseOutgoingIntegrationEvent
{
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string IntegrationEventId { get; set; } = IntegrationEventIdFactory.GenerateIntegrationEventId();
    public required string? CommandId { get; init; }
}