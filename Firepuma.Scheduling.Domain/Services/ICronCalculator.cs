using Firepuma.Scheduling.Domain.Entities;

namespace Firepuma.Scheduling.Domain.Services;

public interface ICronCalculator
{
    DateTime CalculateNextTriggerTime(
        ScheduledTask scheduledTask,
        DateTime startTime,
        bool ignoreValueOfCurrentNextTriggerTime);
}