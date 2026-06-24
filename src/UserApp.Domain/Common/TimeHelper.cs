namespace UserApp.Domain.Common;

public static class TimeHelper
{
    private static TimeZoneInfo? _myanmarTz;

    private static TimeZoneInfo MyanmarTimeZone =>
        _myanmarTz ??= GetMyanmarTimeZone();

    private static TimeZoneInfo GetMyanmarTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Yangon");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Myanmar Standard Time");
        }
    }

    public static DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MyanmarTimeZone);
}
