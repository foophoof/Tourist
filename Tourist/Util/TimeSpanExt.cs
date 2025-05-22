using System.Text;

namespace Tourist.Util;

public static class TimeSpanExt
{
    public static string ToHumanReadable(this TimeSpan span)
    {
        var hours = span.TotalDays > 0
            ? (int)Math.Truncate(span.TotalSeconds / 60 / 60)
            : span.Hours;

        var readable = new StringBuilder();
        if (hours != 0)
        {
            readable.Append($"{hours:00}:");
        }

        readable.Append($"{span.Minutes:00}:");
        readable.Append($"{span.Seconds:00}");

        return readable.ToString();
    }
}
