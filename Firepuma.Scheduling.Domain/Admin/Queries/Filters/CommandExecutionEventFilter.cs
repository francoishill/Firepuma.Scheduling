namespace Firepuma.Scheduling.Domain.Admin.Queries.Filters;

public class CommandExecutionEventFilter
{
    public required string? TextSearch { get; init; } = null!;
    public required SuccessfulFilter? FilterSuccessful { get; init; }
    public required string? FilterTypeName { get; init; }
    public required DateTime? MinimumCreatedOn { get; init; }
    public required DateTime? MaximumCreatedOn { get; init; }
    // public required UserId? ActorId { get; init; }
    public required int? MinimumExecutionSeconds { get; init; }
    public required int? MaximumExecutionSeconds { get; init; }
    public required int? MinimumTotalDuration { get; init; }
    public required int? MaximumTotalDuration { get; init; }

    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public enum SuccessfulFilter
    {
        True,
        False,
        Null,
    }
}