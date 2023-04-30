using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.Domain.Commands;
using Firepuma.Scheduling.Domain.Entities;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;

namespace Firepuma.Scheduling.Domain.IntegrationEvents.IncomingOnly;

// ReSharper disable once UnusedType.Global
[IncomingIntegrationEventType("Firepuma/Request/Scheduling/AddScheduledTask")]
public class AddScheduledTaskRequest
{
    public required string ApplicationId { get; init; }
    public required string EventReplyToAddress { get; init; }

    public required bool IsRecurring { get; init; }

    public DateTime? StartTime { get; init; }

    public int? RecurringUtcOffsetInMinutes { get; init; }
    public string? RecurringCronExpression { get; init; }

    public Dictionary<string, string?>? ExtraValues { get; init; }

    // ReSharper disable once UnusedType.Global
    public class CommandsFactory : ICommandsFactory<AddScheduledTaskRequest>
    {
        public async Task<IEnumerable<ICommandRequest>> Handle(
            CreateCommandsFromIntegrationEventRequest<AddScheduledTaskRequest> request,
            CancellationToken cancellationToken)
        {
            var eventPayload = request.EventPayload;

            var command = new AddScheduledTask
            {
                ApplicationId = eventPayload.ApplicationId,
                EventReplyToAddress = eventPayload.EventReplyToAddress,
                StartTime = eventPayload.StartTime ?? DateTime.UtcNow,
                IsRecurring = eventPayload.IsRecurring,
                RecurringSettings = eventPayload.IsRecurring
                    ? new ScheduledTask.TaskRecurringSettings
                    {
                        UtcOffsetInMinutes = eventPayload.RecurringUtcOffsetInMinutes ?? throw new ArgumentNullException($"{nameof(eventPayload.RecurringUtcOffsetInMinutes)} cannot be null"),
                        CronExpression = eventPayload.RecurringCronExpression!,
                    }
                    : null,
                ExtraValues = eventPayload.ExtraValues ?? new Dictionary<string, string?>(),
            };

            await Task.CompletedTask;
            return new[] { command };
        }
    }
}