using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firepuma.Scheduling.FunctionApp.Api.HttpTriggers.Requests;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Commands;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;
using Firepuma.Scheduling.FunctionApp.Infrastructure.HttpResponses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Firepuma.Scheduling.FunctionApp.Api.HttpTriggers;

public class AddScheduledJob
{
    private readonly IMediator _mediator;

    public AddScheduledJob(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    [FunctionName("AddScheduledJob")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log,
        CancellationToken cancellationToken)
    {
        var correlationId = req.HttpContext?.TraceIdentifier;
        log.LogInformation("C# HTTP trigger function processed a request, correlationId {CorrelationId}", correlationId);

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var requestDto = JsonConvert.DeserializeObject<AddScheduledJobRequest>(requestBody);

        if (requestDto == null)
        {
            return HttpResponseFactory.CreateBadRequestResponse("Request deserialized as NULL");
        }

        if (!requestDto.Validate(out var validationResultsForRequest))
        {
            return HttpResponseFactory.CreateBadRequestResponse("Request is invalid", validationResultsForRequest.Select(s => s.ErrorMessage).ToArray());
        }

        var addCommand = new AddScheduledJobCommand.Payload
        {
            CorrelationId = correlationId,
            ApplicationId = requestDto.ApplicationId,
            StartTime = requestDto.StartTime ?? DateTime.UtcNow,
            IsRecurring = requestDto.IsRecurring ?? throw new ArgumentNullException($"{nameof(requestDto.IsRecurring)} cannot be null"),
            RecurringSettings = requestDto.IsRecurring == true
                ? new ScheduledJob.JobRecurringSettings
                {
                    UtcOffsetInMinutes = requestDto.RecurringUtcOffsetInMinutes ?? throw new ArgumentNullException($"{nameof(requestDto.RecurringUtcOffsetInMinutes)} cannot be null"),
                    CronExpression = requestDto.RecurringCronExpression,
                }
                : null,
            ExtraValues = requestDto.ExtraValues,
        };

        var addResult = await _mediator.Send(addCommand, cancellationToken);

        return new OkObjectResult(addResult);
    }
}