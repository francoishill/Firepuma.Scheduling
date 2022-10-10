using System.Diagnostics;
using Firepuma.DatabaseRepositories.Abstractions.Entities;
using Firepuma.Scheduling.Domain.Features.Scheduling.ValueObjects;
using Newtonsoft.Json.Linq;

namespace Firepuma.Scheduling.Domain.Features.Scheduling.Entities;

[DebuggerDisplay("{DebugDisplay}")]
public class ScheduledJob : BaseEntity
{
    public ClientApplicationId ApplicationId { get; init; }

    public bool IsEnabled { get; set; }

    public DateTime NextTriggerTime { get; set; } = DateTime.MinValue;
    public bool IsRecurring { get; init; }

    public JobRecurringSettings RecurringSettings { get; set; }

    public JObject ExtraValues { get; set; }

    public JobMetadata Metadata { get; set; } = new();

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // ReSharper disable once UnusedMember.Global, EmptyConstructor 
    public ScheduledJob()
    {
        // used by Azure Cosmos deserialization (including the Add methods, like repository.AddItemAsync)
    }

    public void SetTriggerSuccessful(DateTime dateTime)
    {
        Metadata.PreviousTriggeredTime = dateTime;
        Metadata.PreviousTriggerWasSuccessful = true;
        Metadata.PreviousError = null;

        Metadata.TotalSuccessCount++;
        Metadata.ConsecutiveSuccessCount++;

        Metadata.ConsecutiveFailureCount = 0;
    }

    public void SetTriggerFailure(DateTime dateTime, string error)
    {
        Metadata.PreviousTriggeredTime = dateTime;
        Metadata.PreviousTriggerWasSuccessful = false;
        Metadata.PreviousError = error;

        Metadata.ConsecutiveSuccessCount = 0;

        Metadata.TotalFailureCount++;
        Metadata.ConsecutiveFailureCount++;
    }

    public class JobRecurringSettings
    {
        public string CronExpression { get; set; }
        public int UtcOffsetInMinutes { get; set; }
    }

    public class JobMetadata
    {
        public DateTime PreviousTriggeredTime { get; set; } = DateTime.MinValue;
        public bool PreviousTriggerWasSuccessful { get; set; }
        public string PreviousError { get; set; }

        public int TotalSuccessCount { get; set; }
        public int ConsecutiveSuccessCount { get; set; }
        public int TotalFailureCount { get; set; }
        public int ConsecutiveFailureCount { get; set; }
    }

    private string DebugDisplay => $"{ApplicationId}, IsRecurring={IsRecurring}, NextTriggerTime={NextTriggerTime}";
}