using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;

namespace Firepuma.Scheduling.Tests.FunctionApp.Features.Scheduling.Entities;

public class ScheduledJobCalculateNextTriggerTimeTests
{
    [Theory]
    [InlineData("2022-09-01T00:00:00Z", "2022-09-01T00:00:00Z")]
    [InlineData("2022-09-01T00:00:01Z", "2022-09-01T00:00:01Z")]
    [InlineData("2022-09-01T23:59:59Z", "2022-09-01T23:59:59Z")]
    public void OnceOff_ValidCases(
        string startDateTimeUtc,
        string expectedNextTriggerTime)
    {
        // Arrange
        var scheduledJob = new ScheduledJob
        {
            IsRecurring = false,
        };
        var startTime = DateTime.Parse(startDateTimeUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);

        // Act
        scheduledJob.NextTriggerTime = ScheduledJob.CalculateNextTriggerTime(null, scheduledJob, startTime);

        // Assert
        Assert.Equal(DateTime.Parse(expectedNextTriggerTime, null, System.Globalization.DateTimeStyles.RoundtripKind), scheduledJob.NextTriggerTime);
    }

    [Theory]
    [InlineData(0, "*/5 * * * *", "2022-09-01T00:00:00Z", "2022-09-01T00:05:00Z")]
    [InlineData(60, "*/5 * * * *", "2022-09-01T00:00:00Z", "2022-09-01T00:05:00Z")]
    [InlineData(120, "*/5 * * * *", "2022-09-01T00:00:00Z", "2022-09-01T00:05:00Z")]
    [InlineData(840, "*/5 * * * *", "2022-09-01T00:00:00Z", "2022-09-01T00:05:00Z")]
    [InlineData(2, "*/5 * * * *", "2022-09-01T00:00:00Z", "2022-09-01T00:03:00Z")]
    [InlineData(-6, "*/5 * * * *", "2022-09-01T00:00:00Z", "2022-09-01T00:01:00Z")]
    [InlineData(-60, "*/5 * * * *", "2022-09-01T00:00:00Z", "2022-09-01T00:05:00Z")]
    [InlineData(-120, "*/5 * * * *", "2022-09-01T00:00:00Z", "2022-09-01T00:05:00Z")]
    [InlineData(-840, "*/5 * * * *", "2022-09-01T00:00:00Z", "2022-09-01T00:05:00Z")]
    public void Recurring_every_5_minutes_ValidCases(
        int utcOffsetInMinutes,
        string cronExpression,
        string startDateTimeUtc,
        string expectedNextTriggerTime)
    {
        // Arrange
        var scheduledJob = new ScheduledJob
        {
            IsRecurring = true,
            RecurringSettings = new ScheduledJob.JobRecurringSettings
            {
                UtcOffsetInMinutes = utcOffsetInMinutes,
                CronExpression = cronExpression,
            },
        };
        var startTime = DateTime.Parse(startDateTimeUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);

        // Act
        scheduledJob.NextTriggerTime = ScheduledJob.CalculateNextTriggerTime(null, scheduledJob, startTime);

        // Assert
        Assert.Equal(DateTime.Parse(expectedNextTriggerTime, null, System.Globalization.DateTimeStyles.RoundtripKind), scheduledJob.NextTriggerTime);
    }

    [Theory]
    [InlineData(0, "*/15 * * * *", "2022-09-01T00:13:00Z", "2022-09-01T00:15:00Z")]
    [InlineData(3, "*/15 * * * *", "2022-09-01T00:13:00Z", "2022-09-01T00:27:00Z")]
    [InlineData(-3, "*/15 * * * *", "2022-09-01T00:13:00Z", "2022-09-01T00:18:00Z")]
    public void Recurring_every_15_minutes_ValidCases(
        int utcOffsetInMinutes,
        string cronExpression,
        string startDateTimeUtc,
        string expectedNextTriggerTime)
    {
        // Arrange
        var scheduledJob = new ScheduledJob
        {
            IsRecurring = true,
            RecurringSettings = new ScheduledJob.JobRecurringSettings
            {
                UtcOffsetInMinutes = utcOffsetInMinutes,
                CronExpression = cronExpression,
            },
        };
        var startTime = DateTime.Parse(startDateTimeUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);

        // Act
        scheduledJob.NextTriggerTime = ScheduledJob.CalculateNextTriggerTime(null, scheduledJob, startTime);

        // Assert
        Assert.Equal(DateTime.Parse(expectedNextTriggerTime, null, System.Globalization.DateTimeStyles.RoundtripKind), scheduledJob.NextTriggerTime);
    }

    [Theory]
    [InlineData(0, "0 0 * * MON-FRI", "2022-09-01T00:13:00Z", "2022-09-02T00:00:00Z")]
    [InlineData(0, "0 0 * * MON-FRI", "2022-09-02T00:13:00Z", "2022-09-05T00:00:00Z")]
    [InlineData(0, "0 3 * * *", "2022-09-02T00:13:00Z", "2022-09-02T03:00:00Z")]
    [InlineData(-17, "0 0 * * MON-FRI", "2022-09-01T00:13:00Z", "2022-09-01T00:17:00Z")]
    [InlineData(-17, "0 0 * * MON-FRI", "2022-09-02T00:13:00Z", "2022-09-02T00:17:00Z")]
    [InlineData(-17, "0 3 * * *", "2022-09-02T00:13:00Z", "2022-09-02T03:17:00Z")]
    [InlineData(-60, "0 0 * * MON-FRI", "2022-09-01T00:13:00Z", "2022-09-01T01:00:00Z")]
    [InlineData(-60, "0 3 * * *", "2022-09-02T00:13:00Z", "2022-09-02T04:00:00Z")]
    public void Recurring_specific_hour_on_day_ValidCases(
        int utcOffsetInMinutes,
        string cronExpression,
        string startDateTimeUtc,
        string expectedNextTriggerTime)
    {
        // Arrange
        var scheduledJob = new ScheduledJob
        {
            IsRecurring = true,
            RecurringSettings = new ScheduledJob.JobRecurringSettings
            {
                UtcOffsetInMinutes = utcOffsetInMinutes,
                CronExpression = cronExpression,
            },
        };
        var startTime = DateTime.Parse(startDateTimeUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);

        // Act
        scheduledJob.NextTriggerTime = ScheduledJob.CalculateNextTriggerTime(null, scheduledJob, startTime);

        // Assert
        Assert.Equal(DateTime.Parse(expectedNextTriggerTime, null, System.Globalization.DateTimeStyles.RoundtripKind), scheduledJob.NextTriggerTime);
    }
}