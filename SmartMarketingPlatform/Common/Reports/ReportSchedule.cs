namespace SmartMarketingPlatform.Common.Reports;

public sealed class ReportOptions
{
    public const string SectionName = "Reports";

    public bool WorkerEnabled { get; init; } = true;
    public int CheckEveryMinutes { get; init; } = 60;
    public DayOfWeek WeeklyDay { get; init; } = DayOfWeek.Sunday;
    public int WeeklyHour { get; init; } = 9;                   // business local time
    public int MaxBusinessesPerRun { get; init; } = 20;
}

public static class ReportSchedule
{
    // The latest weekly report moment (e.g. Sunday 09:00 business time) that is not in the future, in UTC.
    // The weekly report covers the 7 days that end at this moment.
    public static DateTimeOffset LatestBoundary(DateTimeOffset nowUtc, TimeZoneInfo tz, DayOfWeek day, int hour)
    {
        var local = TimeZoneInfo.ConvertTime(nowUtc, tz).DateTime;
        var daysBack = ((int)local.DayOfWeek - (int)day + 7) % 7;
        var candidate = local.Date.AddDays(-daysBack).AddHours(hour);
        if (candidate > local)
            candidate = candidate.AddDays(-7);
        return new DateTimeOffset(candidate, tz.GetUtcOffset(candidate)).ToUniversalTime();
    }
}
