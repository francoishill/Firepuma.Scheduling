using System.ComponentModel.DataAnnotations;

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Config;

public class IntegrationEventsOptions
{
    public string SelfEventReplyToAddress => $"googlepubsub:{SelfProjectId}/{SelfTopicId}";

    [Required]
    public string SelfProjectId { get; init; } = null!;

    [Required]
    public string SelfTopicId { get; init; } = null!;
}