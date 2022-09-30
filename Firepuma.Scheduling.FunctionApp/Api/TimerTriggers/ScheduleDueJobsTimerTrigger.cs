using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Repositories;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Specifications;
using Firepuma.Scheduling.FunctionApp.Infrastructure.MessageBus;
using Firepuma.Scheduling.FunctionApp.Infrastructure.MessageBus.BusMessages;
using Firepuma.Scheduling.FunctionApp.Infrastructure.MessageBus.Mappings;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Firepuma.Scheduling.FunctionApp.Api.TimerTriggers;

public class ScheduleDueJobsTimerTrigger
{
    private readonly ILogger<ScheduleDueJobsTimerTrigger> _logger;
    private readonly IScheduledJobRepository _scheduledJobRepository;
    private readonly ServiceBusSenderProvider _serviceBusSenderProvider;

    public ScheduleDueJobsTimerTrigger(
        ILogger<ScheduleDueJobsTimerTrigger> logger,
        IScheduledJobRepository scheduledJobRepository,
        ServiceBusSenderProvider serviceBusSenderProvider)
    {
        _logger = logger;
        _scheduledJobRepository = scheduledJobRepository;
        _serviceBusSenderProvider = serviceBusSenderProvider;
    }

    [FunctionName("ScheduleDueJobsTimerTrigger")]
    public async Task RunAsync(
        [TimerTrigger("0 */5 * * * *")] TimerInfo myTimer,
        ILogger log,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# Timer trigger function executed at: {Time}", DateTime.UtcNow);

        var nowWithAddedBufferForProcessingTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(30));
        var querySpecification = new DueSchedulesSpecification(nowWithAddedBufferForProcessingTime);

        var scheduledJobs = await _scheduledJobRepository.GetItemsAsync(querySpecification, cancellationToken);

        var correlationId = Guid.NewGuid().ToString();
        foreach (var scheduledJob in scheduledJobs)
        {
            try
            {
                if (!_serviceBusSenderProvider.TryGetSender(scheduledJob.ApplicationId, out var serviceBusSender))
                {
                    throw new InvalidOperationException($"Unable to find service bus sender for application '{scheduledJob.ApplicationId}' in the registered applications");
                }

                var nextTriggerTime = ScheduledJob.CalculateNextTriggerTime(log, scheduledJob, DateTime.UtcNow.AddSeconds(1));

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
                    scheduledJob.NextTriggerTime = nextTriggerTime;
                }
                else
                {
                    _logger.LogDebug("OnceOff scheduled job {Id} completed, will now disable it", scheduledJob.Id);
                    scheduledJob.IsEnabled = false;
                }

                _logger.LogInformation("Successfully triggered scheduled job {Id} by sending service bus message", scheduledJob.Id);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to successfully trigger scheduled job {Id}, error: {Error}, JSON of job: {Json}",
                    scheduledJob.Id, exception.Message, JsonConvert.SerializeObject(scheduledJob));

                scheduledJob.SetTriggerFailure(DateTime.UtcNow, exception.Message);
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
            }
        }
    }
}