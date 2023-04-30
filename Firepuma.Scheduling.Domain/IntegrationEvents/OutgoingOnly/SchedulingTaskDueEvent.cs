using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;

namespace Firepuma.Scheduling.Domain.IntegrationEvents.OutgoingOnly;

[OutgoingIntegrationEventType("Firepuma/Event/Scheduling/SchedulingTaskDue")]
public class SchedulingTaskDueEvent : BaseOutgoingIntegrationEvent
{
    public required DateTime CurrentTriggerTime { get; init; }
    public required DateTime? NextTriggerTime { get; init; }
    public required bool IsRecurring { get; init; }

    public required TaskRecurringSettings? RecurringSettings { get; init; }

    public Dictionary<string, string?> ExtraValues { get; set; } = new();

    public required TaskMetadata Metadata { get; init; }

    public class TaskRecurringSettings
    {
        public required string CronExpression { get; init; }
        public required int UtcOffsetInMinutes { get; init; }
    }

    public class TaskMetadata
    {
        public required DateTime PreviousTriggeredTime { get; init; }
        public required bool PreviousTriggerWasSuccessful { get; init; }
        public required string? PreviousError { get; set; } = null!;

        public required int TotalSuccessCount { get; set; }
        public required int ConsecutiveSuccessCount { get; set; }
        public required int TotalFailureCount { get; set; }
        public required int ConsecutiveFailureCount { get; set; }
    }
}