using System;
using System.Threading;
using System.Threading.Tasks;
using Firepuma.Scheduling.Domain.Features.Scheduling.Commands;
using Firepuma.Scheduling.Domain.Features.Scheduling.QuerySpecifications;
using Firepuma.Scheduling.Domain.Features.Scheduling.Repositories;
using MediatR;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Firepuma.Scheduling.FunctionApp.Api.TimerTriggers;

public class ScheduleDueJobsTimerTrigger
{
    private readonly IMediator _mediator;
    private readonly IScheduledJobRepository _scheduledJobRepository;

    public ScheduleDueJobsTimerTrigger(
        IMediator mediator,
        IScheduledJobRepository scheduledJobRepository)
    {
        _mediator = mediator;
        _scheduledJobRepository = scheduledJobRepository;
    }

    [FunctionName("ScheduleDueJobsTimerTrigger")]
    public async Task RunAsync(
        [TimerTrigger("0 * * * * *")] TimerInfo myTimer,
        ILogger log,
        CancellationToken cancellationToken)
    {
        log.LogInformation(
            "C# Timer trigger function execution start at: {Time}, IsPastDue: {IsPastDue}, ScheduleStatus: {ScheduleStatus}",
            DateTime.UtcNow.ToString("O"), myTimer.IsPastDue, JsonConvert.SerializeObject(myTimer.ScheduleStatus));

        var checkAheadDuration = TimeSpan.FromSeconds(55);
        var nowWithAddedBufferForProcessingTime = DateTime.UtcNow.Add(checkAheadDuration);
        var querySpecification = new DueSchedulesSpecification(nowWithAddedBufferForProcessingTime);

        var scheduledJobs = await _scheduledJobRepository.GetItemsAsync(querySpecification, cancellationToken);

        var correlationId = Guid.NewGuid().ToString();
        foreach (var scheduledJob in scheduledJobs)
        {
            var notifyClientCommand = new NotifyClientOfDueJobCommand.Payload
            {
                CorrelationId = correlationId,
                ScheduledJob = scheduledJob,
            };

            try
            {
                var result = await _mediator.Send(notifyClientCommand, cancellationToken);

                log.LogInformation(
                    "Client notified of scheduled job {Id}, bus message id {BusMessageId}, bus message type {BusMessageType}",
                    scheduledJob.Id, result.BusMessageId, result.BusMessageTypeName);
            }
            catch (Exception exception)
            {
                log.LogError(
                    exception,
                    "Failed to notify client of due scheduled job {Id}, exception: {Message}",
                    scheduledJob.Id, exception.Message);
            }
        }
    }
}