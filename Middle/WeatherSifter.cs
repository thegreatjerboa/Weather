using System;
using System.Collections.Generic;
using System.Linq;
using Model;

namespace Middle
{
    public static class WeatherSifter
    {
        public static void Prepare(IEnumerable<StationIdentifier> stations, DateTime start, DateTime end)
        {
            if (end < start) throw new ArgumentException("end before start");

            int startYear = start.Year;
            int endYear = end.Year;
            
            int priority = 0;
            foreach (var station in stations)
            {
                int currentYear = startYear;

                while (currentYear <= endYear)
                {
                    WeatherMaker.Instance.Prefetch(new StationYear { StationIdentifier = station, Year = currentYear }, priority);
                    currentYear++;
                }

                priority++;
            }
        }

        public static IEnumerable<WeatherReading> Get(StationIdentifier stationIdentifier, DateTime start, DateTime end)
        {
            if (end < start) throw new ArgumentException("end before start");
            int startYear = start.Year;
            int endYear = end.Year;
            int currentYear = startYear;

            while (currentYear <= endYear)
            {
                WeatherCollection weatherCollection = WeatherMaker.Instance.Get(new StationYear {StationIdentifier = stationIdentifier, Year = currentYear});
                foreach (WeatherReading weather in weatherCollection.WeatherReading.SkipWhile(w => w.ReadingTime < start).TakeWhile(w => w.ReadingTime < end))
                {
                    yield return weather;
                }
                currentYear++;
            }
        }
    }
}
