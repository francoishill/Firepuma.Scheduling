using Cronos;
using Firepuma.Scheduling.Domain.Entities;
using Firepuma.Scheduling.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Firepuma.Scheduling.Infrastructure.Scheduling.Services;

public class CronCalculator : ICronCalculator
{
    private readonly ILogger<CronCalculator> _logger;

    public CronCalculator(
        ILogger<CronCalculator> logger)
    {
        _logger = logger;
    }

    public DateTime CalculateNextTriggerTime(
        ScheduledTask scheduledTask,
        DateTime startTime,
        bool ignoreValueOfCurrentNextTriggerTime)
    {
        if (!scheduledTask.IsRecurring)
        {
            return startTime;
        }

        var expression = CronExpression.Parse(scheduledTask.RecurringSettings!.CronExpression);

        var userTimeZoneInfo = TimeZoneInfo.CreateCustomTimeZone("faketimezone", TimeSpan.FromMinutes(scheduledTask.RecurringSettings.UtcOffsetInMinutes), null, null);

        var fromTime = ignoreValueOfCurrentNextTriggerTime || scheduledTask.NextTriggerTime == DateTime.MinValue
            ? startTime
            : startTime > scheduledTask.NextTriggerTime
                ? startTime
                : scheduledTask.NextTriggerTime.AddMilliseconds(1);

        var nextTriggerTime = expression.GetNextOccurrence(fromTime, userTimeZoneInfo, inclusive: true);

        if (nextTriggerTime == null)
        {
            _logger.LogWarning(
                "Next trigger time is null for scheduled task {Id} with expression '{Expression}' and UTC offset {Offset}, using DateTime.MinValue",
                scheduledTask.Id, scheduledTask.RecurringSettings.CronExpression, scheduledTask.RecurringSettings.UtcOffsetInMinutes);
        }

        return nextTriggerTime ?? DateTime.MinValue;
    }
}