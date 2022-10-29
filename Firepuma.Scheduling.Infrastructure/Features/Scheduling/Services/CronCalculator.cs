using Cronos;
using Firepuma.Scheduling.Domain.Features.Scheduling.Entities;
using Firepuma.Scheduling.Domain.Features.Scheduling.Services;
using Microsoft.Extensions.Logging;

namespace Firepuma.Scheduling.Infrastructure.Features.Scheduling.Services;

public class CronCalculator : ICronCalculator
{
    private readonly ILogger<CronCalculator> _logger;

    public CronCalculator(
        ILogger<CronCalculator> logger)
    {
        _logger = logger;
    }

    public DateTime CalculateNextTriggerTime(
        ScheduledJob scheduledJob,
        DateTime startTime,
        bool ignoreValueOfCurrentNextTriggerTime)
    {
        if (!scheduledJob.IsRecurring)
        {
            return startTime;
        }

        var expression = CronExpression.Parse(scheduledJob.RecurringSettings!.CronExpression);

        var userTimeZoneInfo = TimeZoneInfo.CreateCustomTimeZone("faketimezone", TimeSpan.FromMinutes(scheduledJob.RecurringSettings.UtcOffsetInMinutes), null, null);

        var fromTime = ignoreValueOfCurrentNextTriggerTime || scheduledJob.NextTriggerTime == DateTime.MinValue
            ? startTime
            : startTime > scheduledJob.NextTriggerTime ? startTime : scheduledJob.NextTriggerTime.AddMilliseconds(1);

        var nextTriggerTime = expression.GetNextOccurrence(fromTime, userTimeZoneInfo, inclusive: true);

        if (nextTriggerTime == null)
        {
            _logger.LogWarning(
                "Next trigger time is null for scheduled job {Id} with expression '{Expression}' and UTC offset {Offset}, using DateTime.MinValue",
                scheduledJob.Id, scheduledJob.RecurringSettings.CronExpression, scheduledJob.RecurringSettings.UtcOffsetInMinutes);
        }

        return nextTriggerTime ?? DateTime.MinValue;
    }
}