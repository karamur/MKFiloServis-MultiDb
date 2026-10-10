namespace MKFiloServis.Shared.Time;

/// <summary>
/// Calendar time for business rules whose dates follow the Turkish fleet operation day.
/// Persisted instants should continue to use UTC; this type is only for date boundaries.
/// </summary>
public static class BusinessTime
{
    private static readonly TimeZoneInfo TurkeyTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static DateTime Today => DateAt(DateTime.UtcNow);

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTimeZone);

    public static DateTime LocalAt(DateTime utcInstant)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcInstant, DateTimeKind.Utc), TurkeyTimeZone);

    public static DateTime DateAt(DateTime utcInstant)
        => LocalAt(utcInstant).Date;
}
