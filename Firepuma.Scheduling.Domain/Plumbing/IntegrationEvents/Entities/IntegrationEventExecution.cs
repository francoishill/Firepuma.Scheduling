using Firepuma.DatabaseRepositories.Abstractions.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Entities;

public class IntegrationEventExecution : IEntity
{
    [JsonProperty(PropertyName = "_id")]
    public required string Id { get; set; }

    [JsonProperty(PropertyName = "ETag")]
    public string? ETag { get; set; }

    public required ExecutionStatus Status { get; set; }
    public required string EventId { get; set; }
    public required string TypeName { get; set; }
    public required string TypeNamespace { get; set; }
    public required string Payload { get; set; }
    public required string? SourceCommandId { get; set; }

    public required DateTime StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public double? DurationInSeconds { get; set; }

    public int? SuccessfulCommandCount { get; set; }
    public int? TotalCommandCount { get; set; }
    public string[]? CommandIds { get; set; }

    public ExecutionError[]? Errors { get; set; }
    public int? ErrorCount { get; set; }

    public static string GenerateId() => ObjectId.GenerateNewId().ToString();

    public static IEnumerable<CreateIndexModel<IntegrationEventExecution>> GetSchemaIndexes()
    {
        return new[]
        {
            new CreateIndexModel<IntegrationEventExecution>(
                Builders<IntegrationEventExecution>.IndexKeys.Ascending(p => p.EventId),
                new CreateIndexOptions<IntegrationEventExecution>
                {
                    Unique = true,
                }),
        };
    }

    public static string GetTypeName(Type type)
    {
        if (type.FullName == null)
        {
            return "[NULL_FULLNAME]";
        }

        return type.Namespace == null ? "[NULL_NAMESPACE]" : type.FullName.Substring(type.Namespace.Length + 1);
    }

    public static string GetTypeNamespace(Type type)
    {
        return type.Namespace ?? "NO_NAMESPACE";
    }

    public static string GetSerializedPayload(object eventPayload)
    {
        return JsonConvert.SerializeObject(eventPayload, GetPayloadSerializerSettings());
    }

    private static JsonSerializerSettings GetPayloadSerializerSettings()
    {
        JsonSerializerSettings serializerSettings = new JsonSerializerSettings();
        serializerSettings.Converters.Add(new StringEnumConverter());
        return serializerSettings;
    }

    public enum ExecutionStatus
    {
        New,
        Successful,
        PartiallySuccessful,
        Failed,
    }

    public class ExecutionError
    {
        public required string ErrorMessage { get; set; }
        public required string? ErrorStackTrack { get; set; }
    }
}