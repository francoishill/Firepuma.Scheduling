using System;
using Firepuma.Scheduling.FunctionApp.Abstractions.ClientApplications.ValueObjects;
using Newtonsoft.Json.Linq;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Firepuma.Scheduling.FunctionApp.Infrastructure.MessageBus.BusMessages;

public class SchedulingJobDueBusMessage : ISchedulingBusMessage
{
    public ClientApplicationId ApplicationId { get; init; }

    public DateTime CurrentTriggerTime { get; init; }
    public DateTime? NextTriggerTime { get; init; }
    public bool IsRecurring { get; init; }

    public JobRecurringSettings RecurringSettings { get; set; }

    public JObject ExtraValues { get; set; }

    public JobMetadata Metadata { get; set; }

    public DateTime CreatedOn { get; set; }

    public class JobRecurringSettings
    {
        public string CronExpression { get; set; }
        public int UtcOffsetInMinutes { get; set; }
    }

    public class JobMetadata
    {
        public DateTime PreviousTriggeredTime { get; init; }
        public bool PreviousTriggerWasSuccessful { get; set; }
        public string PreviousError { get; set; }

        public int TotalSuccessCount { get; set; }
        public int ConsecutiveSuccessCount { get; set; }
        public int TotalFailureCount { get; set; }
        public int ConsecutiveFailureCount { get; set; }
    }
}