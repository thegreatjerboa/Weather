using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using Shared;

namespace Middle
{
    public static class WeatherMerger
    {
        public static IEnumerable<StationWeatherReading> Get(StationIdentifier[] stationsIdentifier, DateTime start, DateTime end)
        {
            return Get(stationsIdentifier, start, end, TimeSpan.FromHours(ConfigHelper.DefaultGapFillHours));
        }

        public static IEnumerable<StationWeatherReading> Get(StationIdentifier[] stationsIdentifier, DateTime start, DateTime end, TimeSpan maxGap)
        {
            if (stationsIdentifier == null) throw new ArgumentNullException("stationsIdentifier");
            if (end < start) throw new ArgumentException("end before start");
            if (!stationsIdentifier.Any())
            {
                yield break;
            }

            WeatherSifter.Prepare(stationsIdentifier,start,end);

            DateTime goal = start;
            bool found = false;
            StationIdentifier stationIdentifier = stationsIdentifier.First();


            foreach (WeatherReading weather in WeatherSifter.Get(stationIdentifier, start, end))
            {
                found = true;
                if (weather.ReadingTime.Subtract(goal) > maxGap)
                {
                    //start or middle gap
                    foreach (StationWeatherReading childWeather in Get(stationsIdentifier.Skip(1).ToArray(), goal, weather.ReadingTime, maxGap))
                    {
                        yield return childWeather;
                    }
                }
                //This is the only return from "this" station identifier
                yield return new StationWeatherReading{WeatherReading = weather, Station = stationIdentifier};
                goal = weather.ReadingTime.AddHours(1);
            }
            if (!found)
            {
                //station miss for range
                foreach (StationWeatherReading weather in Get(stationsIdentifier.Skip(1).ToArray(), start, end, maxGap))
                {
                    found = true;
                    yield return weather;
                }
                if (!found)
                {
                }
            }
            else
            {
                //end gap
                if (end.Subtract(goal) > maxGap)
                {
                    foreach (StationWeatherReading childWeather in Get(stationsIdentifier.Skip(1).ToArray(), goal, end, maxGap))
                    {
                        yield return childWeather;
                    }
                }
            }
        }
    }
}
