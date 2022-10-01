using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Repositories;
using Firepuma.Scheduling.FunctionApp.Infrastructure.MessageBus;
using Firepuma.Scheduling.FunctionApp.Infrastructure.MessageBus.BusMessages;
using Firepuma.Scheduling.FunctionApp.Infrastructure.MessageBus.Mappings;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Firepuma.Scheduling.FunctionApp.Features.Scheduling.Commands;

public static class NotifyClientOfDueJobCommand
{
    public class Payload : BaseCommand<Result>
    {
        public string CorrelationId { get; set; }
        public ScheduledJob ScheduledJob { get; init; }
    }

    public class Result
    {
        public string BusMessageId { get; set; }
        public string BusMessageTypeName { get; set; }
    }

    public class Handler : IRequestHandler<Payload, Result>
    {
        private readonly ILogger<Handler> _logger;
        private readonly IScheduledJobRepository _scheduledJobRepository;
        private readonly ServiceBusSenderProvider _serviceBusSenderProvider;

        public Handler(
            ILogger<Handler> logger,
            IScheduledJobRepository scheduledJobRepository,
            ServiceBusSenderProvider serviceBusSenderProvider)
        {
            _logger = logger;
            _scheduledJobRepository = scheduledJobRepository;
            _serviceBusSenderProvider = serviceBusSenderProvider;
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
                if (!_serviceBusSenderProvider.TryGetSender(scheduledJob.ApplicationId, out var serviceBusSender))
                {
                    throw new InvalidOperationException($"Unable to find service bus sender for application '{scheduledJob.ApplicationId}' in the registered applications");
                }

                var nextTriggerTime =
                    scheduledJob.IsRecurring
                        ? ScheduledJob.CalculateNextTriggerTime(_logger, scheduledJob, DateTime.UtcNow.AddSeconds(1))
                        : (DateTime?)null;

                var messageDto = new SchedulingJobDueBusMessage
                {
                    ApplicationId = scheduledJob.ApplicationId,

                    IsRecurring = scheduledJob.IsRecurring,

                    RecurringSettings = scheduledJob.IsRecurring
                        ? new SchedulingJobDueBusMessage.JobRecurringSettings
                        {
                            UtcOffsetInMinutes = scheduledJob.RecurringSettings.UtcOffsetInMinutes,
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

                var messageId = Guid.NewGuid().ToString();
                var messageTypeName = SchedulingBusMessageMappings.GetMessageTypeName(messageDto);

                var busMessage = new ServiceBusMessage(JsonConvert.SerializeObject(messageDto, new Newtonsoft.Json.Converters.StringEnumConverter()))
                {
                    MessageId = messageId,
                    ApplicationProperties =
                    {
                        [SchedulingBusMessageMappings.BUS_MESSAGE_TYPE_PROPERTY_KEY] = messageTypeName,
                    },
                    CorrelationId = correlationId,
                };

                _logger.LogInformation(
                    "Sending message with messageId {MessageId}, messageType {Type}, correlationId {CorrelationId}",
                    busMessage.MessageId, messageTypeName, correlationId);

                await serviceBusSender.SendMessageAsync(busMessage, cancellationToken);

                _logger.LogInformation(
                    "Sent message with messageId {MessageId}, messageType {Type}, correlationId {CorrelationId}",
                    busMessage.MessageId, messageTypeName, correlationId);

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

                successResult.BusMessageId = messageId;
                successResult.BusMessageTypeName = messageTypeName;
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