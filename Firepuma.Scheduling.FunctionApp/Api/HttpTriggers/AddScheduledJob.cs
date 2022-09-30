using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firepuma.Scheduling.FunctionApp.Abstractions.ClientApplications.ValueObjects;
using Firepuma.Scheduling.FunctionApp.Api.HttpTriggers.Requests;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Repositories;
using Firepuma.Scheduling.FunctionApp.Infrastructure.HttpResponses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Firepuma.Scheduling.FunctionApp.Api.HttpTriggers;

public class AddScheduledJob
{
    private readonly IScheduledJobRepository _scheduledJobRepository;

    public AddScheduledJob(
        IScheduledJobRepository scheduledJobRepository)
    {
        _scheduledJobRepository = scheduledJobRepository;
    }

    [FunctionName("AddScheduledJob")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# HTTP trigger function processed a request");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var requestDto = JsonConvert.DeserializeObject<AddScheduledJobRequest>(requestBody);

        if (!requestDto.Validate(out var validationResultsForRequest))
        {
            return HttpResponseFactory.CreateBadRequestResponse("Request is invalid", validationResultsForRequest.Select(s => s.ErrorMessage).ToArray());
        }

        var scheduledJob = new ScheduledJob
        {
            ApplicationId = new ClientApplicationId(requestDto.ApplicationId),

            IsEnabled = true,

            IsRecurring = requestDto.IsRecurring ?? throw new ArgumentNullException($"{nameof(requestDto.IsRecurring)} cannot be null"),

            ExtraValues = requestDto.ExtraValues,
        };

        if (scheduledJob.IsRecurring)
        {
            scheduledJob.RecurringSettings = new ScheduledJob.JobRecurringSettings
            {
                UtcOffsetInMinutes = requestDto.RecurringUtcOffsetInMinutes ?? throw new ArgumentNullException($"{nameof(requestDto.RecurringUtcOffsetInMinutes)} cannot be null"),
                CronExpression = requestDto.RecurringCronExpression,
            };
        }

        scheduledJob.NextTriggerTime = ScheduledJob.CalculateNextTriggerTime(log, scheduledJob, requestDto.StartTime ?? DateTime.UtcNow);

        await _scheduledJobRepository.AddItemAsync(scheduledJob, cancellationToken);

        return new OkObjectResult(new
        {
            ScheduledJobId = scheduledJob.Id,
        });
    }
}