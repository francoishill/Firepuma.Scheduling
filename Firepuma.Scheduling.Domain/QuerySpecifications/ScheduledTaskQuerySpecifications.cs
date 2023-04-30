using Firepuma.DatabaseRepositories.Abstractions.QuerySpecifications;
using Firepuma.Scheduling.Domain.Entities;

namespace Firepuma.Scheduling.Domain.QuerySpecifications;

public static class ScheduledTaskQuerySpecifications
{
    public class DueNow : QuerySpecification<ScheduledTask>
    {
        public DueNow(DateTime utcNow)
        {
            WhereExpressions.Add(schedule =>
                schedule.IsEnabled &&
                schedule.NextTriggerTime < utcNow);
        }
    }
}