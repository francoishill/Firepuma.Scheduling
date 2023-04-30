using Firepuma.Scheduling.Domain.IntegrationEvents;

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;

/// <summary>
/// Outgoing IntegrationEvents should add this attribute on their DTO class and pass in a unique value for
/// the IntegrationEventType, an example is <see cref="ScheduledNotifyDueTasks"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class OutgoingIntegrationEventTypeAttribute : Attribute
{
    public string IntegrationEventType { get; }

    public OutgoingIntegrationEventTypeAttribute(string integrationEventType)
    {
        IntegrationEventType = integrationEventType;
    }
}