using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.Domain.Features.Scheduling.Entities;
using Firepuma.Scheduling.Domain.Features.Scheduling.Repositories;
using Firepuma.Scheduling.Domain.Features.Scheduling.Services;
using Firepuma.Scheduling.Domain.Features.Scheduling.ValueObjects;
using MediatR;
using Newtonsoft.Json.Linq;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Firepuma.Scheduling.Domain.Features.Scheduling.Commands;

public static class AddScheduledJobCommand
{
    public class Payload : BaseCommand<Result>
    {
        public string? CorrelationId { get; set; }

        public string ApplicationId { get; set; } = null!;

        public DateTime StartTime { get; set; }

        public bool IsRecurring { get; set; }

        public ScheduledJob.JobRecurringSettings? RecurringSettings { get; set; } = null!;

        public JObject? ExtraValues { get; set; } = null!;
    }

    public class Result
    {
        public string ScheduledJobId { get; set; } = null!;
    }

    public class Handler : IRequestHandler<Payload, Result>
    {
        private readonly IScheduledJobRepository _scheduledJobRepository;
        private readonly ICronCalculator _cronCalculator;

        public Handler(
            IScheduledJobRepository scheduledJobRepository,
            ICronCalculator cronCalculator)
        {
            _scheduledJobRepository = scheduledJobRepository;
            _cronCalculator = cronCalculator;
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

            scheduledJob.NextTriggerTime = _cronCalculator.CalculateNextTriggerTime(scheduledJob, payload.StartTime);

            await _scheduledJobRepository.AddItemAsync(scheduledJob, cancellationToken);

            return new Result
            {
                ScheduledJobId = scheduledJob.Id,
            };
        }
    }
}