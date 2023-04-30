using System.Diagnostics;
using Firepuma.DatabaseRepositories.MongoDb.Abstractions.Entities;
using Firepuma.Scheduling.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Firepuma.Scheduling.Domain.Entities;

[DebuggerDisplay("{DebugDisplay}")]
public class ScheduledTask : BaseMongoDbEntity
{
    public required ClientApplicationId ApplicationId { get; init; }
    public required string EventReplyToAddress { get; init; }

    public required bool IsEnabled { get; set; }

    public DateTime NextTriggerTime { get; set; } = DateTime.MinValue;
    public required bool IsRecurring { get; init; }

    public required TaskRecurringSettings? RecurringSettings { get; init; }

    public required Dictionary<string, string?> ExtraValues { get; init; }

    public TaskMetadata Metadata { get; init; } = new();

    public DateTime CreatedOn { get; init; } = DateTime.UtcNow;

    public static string GenerateId() => ObjectId.GenerateNewId().ToString();

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

    public class TaskRecurringSettings
    {
        public required string CronExpression { get; init; }
        public required int UtcOffsetInMinutes { get; init; }
    }

    public class TaskMetadata
    {
        public DateTime PreviousTriggeredTime { get; set; } = DateTime.MinValue;
        public bool PreviousTriggerWasSuccessful { get; set; }
        public string? PreviousError { get; set; }

        public int TotalSuccessCount { get; set; }
        public int ConsecutiveSuccessCount { get; set; }
        public int TotalFailureCount { get; set; }
        public int ConsecutiveFailureCount { get; set; }
    }

    private string DebugDisplay => $"{ApplicationId}, IsRecurring={IsRecurring}, NextTriggerTime={NextTriggerTime}";

    public static IEnumerable<CreateIndexModel<ScheduledTask>> GetSchemaIndexes()
    {
        return Array.Empty<CreateIndexModel<ScheduledTask>>();
    }
}