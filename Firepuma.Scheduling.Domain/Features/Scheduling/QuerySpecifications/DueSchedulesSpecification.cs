using Firepuma.DatabaseRepositories.Abstractions.QuerySpecifications;
using Firepuma.Scheduling.Domain.Features.Scheduling.Entities;

namespace Firepuma.Scheduling.Domain.Features.Scheduling.QuerySpecifications;

public class DueSchedulesSpecification : QuerySpecification<ScheduledJob>
{
    public DueSchedulesSpecification(DateTime utcNow)
    {
        WhereExpressions.Add(schedule =>
            schedule.IsEnabled &&
            schedule.NextTriggerTime < utcNow);
    }
}