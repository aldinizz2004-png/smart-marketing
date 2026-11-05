using SmartMarketingPlatform.Common.Reports;

namespace SmartMarketingPlatform.UnitTests.Reports;

public class ReportScheduleTests
{
    private static readonly TimeZoneInfo Hebron = TimeZoneInfo.FindSystemTimeZoneById("Asia/Hebron");

    private static DateTimeOffset Local(int year, int month, int day, int hour, int minute = 0)
    {
        var local = new DateTime(year, month, day, hour, minute, 0);
        return new DateTimeOffset(local, Hebron.GetUtcOffset(local));
    }

    [Fact]
    public void On_wednesday_the_latest_boundary_is_last_sunday_morning()
    {
        var now = Local(2026, 11, 4, 13, 0);

        var boundary = ReportSchedule.LatestBoundary(now.ToUniversalTime(), Hebron, DayOfWeek.Sunday, 9);

        Assert.Equal(Local(2026, 11, 1, 9, 0), boundary);
    }

    [Fact]
    public void Before_the_hour_on_report_day_the_previous_week_is_used()
    {
        var now = Local(2026, 11, 8, 8, 59);

        var boundary = ReportSchedule.LatestBoundary(now.ToUniversalTime(), Hebron, DayOfWeek.Sunday, 9);

        Assert.Equal(Local(2026, 11, 1, 9, 0), boundary);
    }

    [Fact]
    public void At_the_exact_report_moment_the_new_week_starts()
    {
        var now = Local(2026, 11, 8, 9, 0);

        var boundary = ReportSchedule.LatestBoundary(now.ToUniversalTime(), Hebron, DayOfWeek.Sunday, 9);

        Assert.Equal(Local(2026, 11, 8, 9, 0), boundary);
    }

    [Fact]
    public void The_boundary_is_in_business_time_not_utc()
    {
        // 23:30 UTC on Saturday is already Sunday 01:30 in Hebron (UTC+2).
        var now = new DateTimeOffset(2026, 11, 7, 23, 30, 0, TimeSpan.Zero);

        var boundary = ReportSchedule.LatestBoundary(now, Hebron, DayOfWeek.Sunday, 0);

        Assert.Equal(Local(2026, 11, 8, 0, 0), boundary);
    }
}
