namespace Firepuma.Scheduling.Domain.Infrastructure.MessageBus.Results;

public class SentMessageToApplicationResult
{
    public string MessageId { get; init; } = null!;
    public string MessageTypeName { get; init; } = null!;
}