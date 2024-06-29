using System;

public static class TimeUtil
{
    public static string SecondsToString(double seconds)
    {
        int hours = (int)Math.Floor(seconds / 3600);
        int minutes = (int)Math.Floor((seconds - (hours * 3600)) / 60);

        if (hours > 0)
        {
            return $"{hours}h";
        }
        if (minutes > 0)
        {
            seconds -= (minutes * 60);
            return $"{minutes}m";
        }
        return $"{seconds:F0}s";
    }

    public static double NowS()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
