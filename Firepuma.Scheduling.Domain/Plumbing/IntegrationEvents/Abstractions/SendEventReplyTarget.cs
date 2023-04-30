namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;

public class SendEventReplyTarget : ISendEventTarget
{
    public required string EventReplyToAddress { get; init; }
}