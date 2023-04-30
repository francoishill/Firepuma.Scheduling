using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.Domain.Entities;
using Firepuma.Scheduling.Domain.Repositories;
using Firepuma.Scheduling.Domain.Services;
using Firepuma.Scheduling.Domain.ValueObjects;
using MediatR;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Firepuma.Scheduling.Domain.Commands;

public class AddScheduledTask : BaseCommand<AddScheduledTask.Result>
{
    public required string ApplicationId { get; init; }
    public required string EventReplyToAddress { get; init; }

    public required DateTime StartTime { get; init; }

    public required bool IsRecurring { get; init; }

    public required ScheduledTask.TaskRecurringSettings? RecurringSettings { get; init; }

    public required Dictionary<string, string?> ExtraValues { get; init; }


    public class Result
    {
        public required string ScheduledTaskId { get; init; }
    }

    public class Handler : IRequestHandler<AddScheduledTask, Result>
    {
        private readonly IScheduledTaskRepository _scheduledTaskRepository;
        private readonly ICronCalculator _cronCalculator;

        public Handler(
            IScheduledTaskRepository scheduledTaskRepository,
            ICronCalculator cronCalculator)
        {
            _scheduledTaskRepository = scheduledTaskRepository;
            _cronCalculator = cronCalculator;
        }

        public async Task<Result> Handle(
            AddScheduledTask payload,
            CancellationToken cancellationToken)
        {
            var scheduledTask = new ScheduledTask
            {
                Id = ScheduledTask.GenerateId(),
                ApplicationId = new ClientApplicationId(payload.ApplicationId),
                EventReplyToAddress = payload.EventReplyToAddress,

                IsEnabled = true,

                IsRecurring = payload.IsRecurring,
                RecurringSettings = payload.IsRecurring ? payload.RecurringSettings : null,

                ExtraValues = payload.ExtraValues,
            };

            scheduledTask.NextTriggerTime = _cronCalculator.CalculateNextTriggerTime(scheduledTask, payload.StartTime, false);

            await _scheduledTaskRepository.AddItemAsync(scheduledTask, cancellationToken);

            return new Result
            {
                ScheduledTaskId = scheduledTask.Id,
            };
        }
    }
}