using System;
using System.Linq;
using Model;

namespace Middle
{
    public static class StationFinder
    {
        public static StationIdentifier[] Find(double lat, double lon, int maxNumber = 50, int maxMiles = 100)
        {
            const double milesPerEquatorDegree = 69.17059f;
            double smashFactor = Math.Cos(Math.PI * lat / 180.0);
            
            return StationMaker.Instance.Get()
                    .Select(ws => new
                    {
                        Station = ws,
                        Distance = milesPerEquatorDegree * Math.Sqrt(Math.Pow(lon - ws.Longitude, 2) + Math.Pow(smashFactor * (lat - ws.Latitude), 2))
                    })
                    .Where(rangeStation => rangeStation.Distance <= maxMiles)
                    .Take(maxNumber)
                    .OrderBy(rangeStation => rangeStation.Distance)
                    .Select(ws => ws.Station.Identifier)
                    .ToArray();
        }
    }
}
