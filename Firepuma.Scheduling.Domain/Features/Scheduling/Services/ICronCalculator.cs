using Firepuma.Scheduling.Domain.Features.Scheduling.Entities;

namespace Firepuma.Scheduling.Domain.Features.Scheduling.Services;

public interface ICronCalculator
{
    DateTime CalculateNextTriggerTime(
        ScheduledJob scheduledJob,
        DateTime startTime);
}