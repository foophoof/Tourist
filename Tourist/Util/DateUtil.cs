namespace Tourist.Util;

public static class DateUtil
{
    internal static DateTimeOffset EorzeaTime(DateTimeOffset? at = null)
    {
        at ??= DateTimeOffset.UtcNow;
        return DateTimeOffset.FromUnixTimeMilliseconds(at.Value.ToUnixTimeMilliseconds() * 144 / 7);
    }

    internal static DateTimeOffset EarthTime(DateTimeOffset eorzea)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(eorzea.ToUnixTimeMilliseconds() * 7 / 144);
    }
}
