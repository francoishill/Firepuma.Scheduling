using System;
using Firepuma.Scheduling.FunctionApp.Abstractions.Specifications;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;

namespace Firepuma.Scheduling.FunctionApp.Features.Scheduling.Specifications;

public class DueSchedulesSpecification : Specification<ScheduledJob>
{
    public DueSchedulesSpecification(DateTime utcNow)
    {
        WhereExpressions.Add(schedule =>
            schedule.IsEnabled &&
            schedule.NextTriggerTime < utcNow);
    }
}