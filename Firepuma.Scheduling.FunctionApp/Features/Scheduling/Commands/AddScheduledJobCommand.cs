using System;
using System.Threading;
using System.Threading.Tasks;
using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.FunctionApp.Abstractions.ClientApplications.ValueObjects;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Firepuma.Scheduling.FunctionApp.Features.Scheduling.Commands;

public static class AddScheduledJobCommand
{
    public class Payload : BaseCommand<Result>
    {
        public string CorrelationId { get; set; }

        public string ApplicationId { get; set; }

        public DateTime StartTime { get; set; }

        public bool IsRecurring { get; set; }

        public ScheduledJob.JobRecurringSettings RecurringSettings { get; set; }

        public JObject ExtraValues { get; set; }
    }

    public class Result
    {
        public string ScheduledJobId { get; set; }
    }

    public class Handler : IRequestHandler<Payload, Result>
    {
        private readonly ILogger<Handler> _logger;
        private readonly IScheduledJobRepository _scheduledJobRepository;

        public Handler(
            ILogger<Handler> logger,
            IScheduledJobRepository scheduledJobRepository)
        {
            _logger = logger;
            _scheduledJobRepository = scheduledJobRepository;
        }

        public async Task<Result> Handle(
            Payload payload,
            CancellationToken cancellationToken)
        {
            var scheduledJob = new ScheduledJob
            {
                ApplicationId = new ClientApplicationId(payload.ApplicationId),

                IsEnabled = true,

                IsRecurring = payload.IsRecurring,

                ExtraValues = payload.ExtraValues,
            };

            if (scheduledJob.IsRecurring)
            {
                scheduledJob.RecurringSettings = payload.RecurringSettings;
            }

            scheduledJob.NextTriggerTime = ScheduledJob.CalculateNextTriggerTime(_logger, scheduledJob, payload.StartTime);

            await _scheduledJobRepository.AddItemAsync(scheduledJob, cancellationToken);

            return new Result
            {
                ScheduledJobId = scheduledJob.Id,
            };
        }
    }
}