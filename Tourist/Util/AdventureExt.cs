﻿using FFXIVWeather.Lumina;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;

namespace Tourist.Util;

public static class AdventureExt
{
    private static Dictionary<uint, (DateTimeOffset start, DateTimeOffset end)> Availability { get; } = new();

    public static (DateTimeOffset start, DateTimeOffset end)? NextAvailable(this Adventure adventure, FFXIVWeatherLuminaService service)
    {
        if (adventure is {MinTime: 0, MaxTime: 0})
        {
            return null;
        }

        var actualNow = DateTimeOffset.UtcNow;

        var contains = Availability.TryGetValue(adventure.RowId, out var cached);

        switch (contains)
        {
            // if the cache doesn't have this vista but it's currently available
            case false when adventure.Available(service):
                {
                    // determine the end availability and store that
                    var ends = adventure.AvailabilityEnds(service, DateTimeOffset.Now) ?? default;
                    Availability[adventure.RowId] = (DateTimeOffset.Now, ends);
                    break;
                }

            // use the cached value if it hasn't expired
            case true when cached.end >= actualNow:
                return cached;
        }

        // otherwise, calculate and cache the availability

        var eorzea = DateUtil.EorzeaTime(actualNow);
        // start at a clean hour
        eorzea = new DateTimeOffset(eorzea.Year, eorzea.Month, eorzea.Day, eorzea.Hour, 0, 0, 0, eorzea.Offset);

        var minHour = adventure.MinTime / 100;
        var maxHour = adventure.MaxTime / 100 + 1;
        var numHours = (24 + maxHour - minHour) % 24;

        for (var i = 0; i < 10_000; i++)
        {
            // find the next available time
            var add = (minHour + 24 - eorzea.Hour) % 24;
            eorzea = eorzea.AddHours(add);

            // check the weather for each hour in the available range
            for (var h = 0; h < numHours; h++)
            {
                var earth = DateUtil.EarthTime(eorzea);

                // check the weather
                var offset = Math.Ceiling((earth - actualNow).TotalSeconds);
                if (adventure.WeatherAvailable(service, offset))
                {
                    // determine when the availability will end
                    var ends = adventure.AvailabilityEnds(service, earth) ?? default;
                    // cache the result of the calculation
                    Availability[adventure.RowId] = (earth, ends);
                    return (earth, ends);
                }

                eorzea = eorzea.AddHours(1);
            }
        }

        return null;
    }

    private static DateTimeOffset? AvailabilityEnds(this Adventure adventure, FFXIVWeatherLuminaService service, DateTimeOffset starting)
    {
        if (adventure is {MinTime: 0, MaxTime: 0})
        {
            return null;
        }

        var now = starting;

        var eorzea = DateUtil.EorzeaTime(now);
        eorzea = new DateTimeOffset(eorzea.Year, eorzea.Month, eorzea.Day, eorzea.Hour, 0, 0, 0, eorzea.Offset);
        now = DateUtil.EarthTime(eorzea);

        var maxHour = adventure.MaxTime / 100 + 1;

        while (eorzea.Hour != maxHour)
        {
            if (!adventure.Available(service, eorzea))
            {
                return now;
            }

            eorzea = eorzea.AddHours(1);
            now = DateUtil.EarthTime(eorzea);
        }

        return now;
    }

    public static bool Available(this Adventure adventure, FFXIVWeatherLuminaService service, DateTimeOffset? eorzea = null)
    {
        var time = adventure.TimeAvailable(eorzea);

        var offset = eorzea == null
            ? 0
            : Math.Ceiling((DateUtil.EarthTime(eorzea.Value) - DateTimeOffset.UtcNow).TotalSeconds);
        var weather = adventure.WeatherAvailable(service, offset);

        return time && weather;
    }

    private static bool TimeAvailable(this Adventure adventure, DateTimeOffset? eorzea = null)
    {
        if (adventure is {MinTime: 0, MaxTime: 0})
        {
            return true;
        }

        eorzea ??= DateUtil.EorzeaTime();

        var minHour = adventure.MinTime / 100;
        var minMins = adventure.MinTime % 100;
        var min = new TimeSpan(minHour, minMins, 0);

        var maxHour = adventure.MaxTime / 100;
        var maxMins = adventure.MaxTime % 100;
        var max = new TimeSpan(maxHour, maxMins, 59);

        var now = eorzea.Value.TimeOfDay;

        if (min <= max)
        {
            // start and stop times are in the same day
            if (now >= min && now <= max)
            {
                return true;
            }
        }
        else
        {
            // start and stop times are in different days
            if (now >= min || now <= max)
            {
                return true;
            }
        }

        return false;
    }

    private static bool WeatherAvailable(this Adventure adventure, FFXIVWeatherLuminaService service, double offset = 0d)
    {
        if (!Weathers.All.TryGetValue(adventure.RowId, out var weathers))
        {
            return true;
        }

        var (weather, _) = service.GetCurrentWeather(adventure.Level.Value.Territory.Value, offset);

        return weathers.Contains(weather.RowId);
    }
}
