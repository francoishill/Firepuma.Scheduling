using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.Domain.Entities;
using Firepuma.Scheduling.Domain.IntegrationEvents.OutgoingOnly;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;
using Firepuma.Scheduling.Domain.Repositories;
using Firepuma.Scheduling.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Firepuma.Scheduling.Domain.Commands;

public class NotifyDueTask : BaseCommand<NotifyDueTask.Result>
{
    public required ScheduledTask ScheduledTask { get; init; }

    public class Result
    {
        public required string IntegrationEventId { get; set; }
    }

    public class Handler : IRequestHandler<NotifyDueTask, Result>
    {
        private readonly ILogger<Handler> _logger;
        private readonly IScheduledTaskRepository _scheduledTaskRepository;
        private readonly ICronCalculator _cronCalculator;
        private readonly ICommandEventPublisher _commandEventPublisher;

        public Handler(
            ILogger<Handler> logger,
            IScheduledTaskRepository scheduledTaskRepository,
            ICronCalculator cronCalculator,
            ICommandEventPublisher commandEventPublisher)
        {
            _logger = logger;
            _scheduledTaskRepository = scheduledTaskRepository;
            _cronCalculator = cronCalculator;
            _commandEventPublisher = commandEventPublisher;
        }

        public async Task<Result> Handle(
            NotifyDueTask payload,
            CancellationToken cancellationToken)
        {
            var scheduledTask = payload.ScheduledTask;

            var failureMessages = new List<string>();
            Result? successResult = null;

            try
            {
                var nextTriggerTime =
                    scheduledTask.IsRecurring
                        ? _cronCalculator.CalculateNextTriggerTime(scheduledTask, DateTime.UtcNow.AddSeconds(1), false)
                        : (DateTime?)null;

                var integrationEvent = new SchedulingTaskDueEvent
                {
                    CommandId = payload.CommandId,

                    IsRecurring = scheduledTask.IsRecurring,

                    RecurringSettings = scheduledTask.IsRecurring
                        ? new SchedulingTaskDueEvent.TaskRecurringSettings
                        {
                            UtcOffsetInMinutes = scheduledTask.RecurringSettings!.UtcOffsetInMinutes,
                            CronExpression = scheduledTask.RecurringSettings.CronExpression,
                        }
                        : null,

                    CurrentTriggerTime = scheduledTask.NextTriggerTime,
                    NextTriggerTime = nextTriggerTime,

                    ExtraValues = scheduledTask.ExtraValues,

                    Metadata = new SchedulingTaskDueEvent.TaskMetadata
                    {
                        PreviousTriggeredTime = scheduledTask.Metadata.PreviousTriggeredTime,
                        PreviousTriggerWasSuccessful = scheduledTask.Metadata.PreviousTriggerWasSuccessful,
                        PreviousError = scheduledTask.Metadata.PreviousError,
                        TotalSuccessCount = scheduledTask.Metadata.TotalSuccessCount,
                        ConsecutiveSuccessCount = scheduledTask.Metadata.ConsecutiveSuccessCount,
                        TotalFailureCount = scheduledTask.Metadata.TotalFailureCount,
                        ConsecutiveFailureCount = scheduledTask.Metadata.ConsecutiveFailureCount,
                    },

                    CreatedOn = scheduledTask.CreatedOn,
                };

                await _commandEventPublisher.PublishAsync(
                    payload,
                    integrationEvent,
                    new SendEventReplyTarget { EventReplyToAddress = scheduledTask.EventReplyToAddress },
                    cancellationToken);

                scheduledTask.SetTriggerSuccessful(DateTime.UtcNow);

                if (scheduledTask.IsRecurring)
                {
                    scheduledTask.NextTriggerTime = nextTriggerTime ?? DateTime.MinValue;
                }
                else
                {
                    _logger.LogDebug("OnceOff scheduled task {Id} completed, will now disable it", scheduledTask.Id);
                    scheduledTask.IsEnabled = false;
                }

                _logger.LogInformation("Successfully triggered scheduled task {Id} by sending service bus message", scheduledTask.Id);

                successResult = new Result
                {
                    IntegrationEventId = integrationEvent.IntegrationEventId,
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to successfully trigger scheduled task {Id}, error: {Error}, JSON of task: {Json}",
                    scheduledTask.Id, exception.Message, JsonConvert.SerializeObject(scheduledTask));

                scheduledTask.SetTriggerFailure(DateTime.UtcNow, exception.Message);

                failureMessages.Add($"Failed to successfully trigger scheduled task {scheduledTask.Id}, error: {exception.Message}");
            }

            try
            {
                await _scheduledTaskRepository.ReplaceItemAsync(scheduledTask, cancellationToken);

                _logger.LogInformation("Successfully updated scheduled task {Id}", scheduledTask.Id);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to update scheduled task {Id}, error: {Error}, JSON of task: {Json}",
                    scheduledTask.Id, exception.Message, JsonConvert.SerializeObject(scheduledTask));

                failureMessages.Add($"Unable to update scheduled task {scheduledTask.Id}, error: {exception.Message}");
            }

            if (failureMessages.Count > 0)
            {
                throw new Exception(string.Join(". ", failureMessages));
            }

            return successResult!;
        }
    }
}