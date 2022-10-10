namespace Firepuma.Scheduling.Domain.Infrastructure.MessageBus.Results;

public class SentMessageToApplicationResult
{
    public string MessageId { get; init; }
    public string MessageTypeName { get; init; }
}