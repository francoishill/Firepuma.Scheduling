using Firepuma.Scheduling.Domain.IntegrationEvents.IncomingOnly;

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;

/// <summary>
/// Incoming IntegrationEvents should add this attribute on their DTO class and pass in a unique value for
/// the IntegrationEventType, an example is <see cref="AddScheduledTaskRequest"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class IncomingIntegrationEventTypeAttribute : Attribute
{
    public string IntegrationEventType { get; }

    public IncomingIntegrationEventTypeAttribute(string integrationEventType)
    {
        IntegrationEventType = integrationEventType;
    }
}