using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.Domain.Features.Scheduling.Entities;
using Firepuma.Scheduling.Domain.Features.Scheduling.Repositories;
using Firepuma.Scheduling.Domain.Features.Scheduling.Services;
using Firepuma.Scheduling.Domain.Infrastructure.MessageBus;
using Firepuma.Scheduling.Domain.Infrastructure.MessageBus.BusMessages;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Firepuma.Scheduling.Domain.Features.Scheduling.Commands;

public static class NotifyClientOfDueJobCommand
{
    public class Payload : BaseCommand<Result>
    {
        public string CorrelationId { get; set; } = null!;
        public ScheduledJob ScheduledJob { get; init; } = null!;
    }

    public class Result
    {
        public string BusMessageId { get; set; } = null!;
        public string BusMessageTypeName { get; set; } = null!;
    }

    public class Handler : IRequestHandler<Payload, Result>
    {
        private readonly ILogger<Handler> _logger;
        private readonly IScheduledJobRepository _scheduledJobRepository;
        private readonly ICronCalculator _cronCalculator;
        private readonly IClientAppBusMessageSender _clientAppBusMessageSender;

        public Handler(
            ILogger<Handler> logger,
            IScheduledJobRepository scheduledJobRepository,
            ICronCalculator cronCalculator,
            IClientAppBusMessageSender clientAppBusMessageSender)
        {
            _logger = logger;
            _scheduledJobRepository = scheduledJobRepository;
            _cronCalculator = cronCalculator;
            _clientAppBusMessageSender = clientAppBusMessageSender;
        }

        public async Task<Result> Handle(
            Payload payload,
            CancellationToken cancellationToken)
        {
            var correlationId = payload.CorrelationId;
            var scheduledJob = payload.ScheduledJob;

            var failureMessages = new List<string>();
            var successResult = new Result();

            try
            {
                var nextTriggerTime =
                    scheduledJob.IsRecurring
                        ? _cronCalculator.CalculateNextTriggerTime(scheduledJob, DateTime.UtcNow.AddSeconds(1), false)
                        : (DateTime?)null;

                var messageDto = new SchedulingJobDueBusMessage
                {
                    ApplicationId = scheduledJob.ApplicationId,

                    IsRecurring = scheduledJob.IsRecurring,

                    RecurringSettings = scheduledJob.IsRecurring
                        ? new SchedulingJobDueBusMessage.JobRecurringSettings
                        {
                            UtcOffsetInMinutes = scheduledJob.RecurringSettings!.UtcOffsetInMinutes,
                            CronExpression = scheduledJob.RecurringSettings.CronExpression,
                        }
                        : null,

                    CurrentTriggerTime = scheduledJob.NextTriggerTime,
                    NextTriggerTime = nextTriggerTime,

                    ExtraValues = scheduledJob.ExtraValues,

                    Metadata = new SchedulingJobDueBusMessage.JobMetadata
                    {
                        PreviousTriggeredTime = scheduledJob.Metadata.PreviousTriggeredTime,
                        PreviousTriggerWasSuccessful = scheduledJob.Metadata.PreviousTriggerWasSuccessful,
                        PreviousError = scheduledJob.Metadata.PreviousError,
                        TotalSuccessCount = scheduledJob.Metadata.TotalSuccessCount,
                        ConsecutiveSuccessCount = scheduledJob.Metadata.ConsecutiveSuccessCount,
                        TotalFailureCount = scheduledJob.Metadata.TotalFailureCount,
                        ConsecutiveFailureCount = scheduledJob.Metadata.ConsecutiveFailureCount,
                    },

                    CreatedOn = scheduledJob.CreatedOn,
                };

                var sentMessageResult = await _clientAppBusMessageSender.SendMessageToApplicationAsync(
                    scheduledJob.ApplicationId,
                    correlationId,
                    messageDto,
                    cancellationToken);

                scheduledJob.SetTriggerSuccessful(DateTime.UtcNow);

                if (scheduledJob.IsRecurring)
                {
                    scheduledJob.NextTriggerTime = nextTriggerTime ?? DateTime.MinValue;
                }
                else
                {
                    _logger.LogDebug("OnceOff scheduled job {Id} completed, will now disable it", scheduledJob.Id);
                    scheduledJob.IsEnabled = false;
                }

                _logger.LogInformation("Successfully triggered scheduled job {Id} by sending service bus message", scheduledJob.Id);

                successResult.BusMessageId = sentMessageResult.MessageId;
                successResult.BusMessageTypeName = sentMessageResult.MessageTypeName;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to successfully trigger scheduled job {Id}, error: {Error}, JSON of job: {Json}",
                    scheduledJob.Id, exception.Message, JsonConvert.SerializeObject(scheduledJob));

                scheduledJob.SetTriggerFailure(DateTime.UtcNow, exception.Message);

                failureMessages.Add($"Failed to successfully trigger scheduled job {scheduledJob.Id}, error: {exception.Message}");
            }

            try
            {
                await _scheduledJobRepository.UpsertItemAsync(scheduledJob, cancellationToken: cancellationToken);

                _logger.LogInformation("Successfully updated scheduled job {Id}", scheduledJob.Id);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to update scheduled job {Id}, error: {Error}, JSON of job: {Json}",
                    scheduledJob.Id, exception.Message, JsonConvert.SerializeObject(scheduledJob));

                failureMessages.Add($"Unable to update scheduled job {scheduledJob.Id}, error: {exception.Message}");
            }

            if (failureMessages.Count > 0)
            {
                throw new Exception(string.Join(". ", failureMessages));
            }

            return successResult;
        }
    }
}