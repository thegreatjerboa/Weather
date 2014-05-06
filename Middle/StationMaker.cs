using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Model;
using ProtoBuf;

namespace Middle
{
    public sealed class StationMaker
    {
        private static readonly Lazy<StationMaker> Lazy = new Lazy<StationMaker>(() => new StationMaker());

        private readonly object _lock = new object();
        private Task<StationCollection> _cache;
        private StationMaker() {}

        public static StationMaker Instance
        {
            get { return Lazy.Value; }
        }

        public Station[] Get()
        {
            return GetObservable().Result.Stations;
        }

        public Station Get(StationIdentifier identifier)
        {
            return Get().FirstOrDefault(station => station.Identifier == identifier);
        }

        public void Prefetch()
        {
            GetObservable();
        }

        private Task<StationCollection> GetObservable()
        {
            lock (_lock)
            {
                if (_cache != null)
                {
                    if (_cache.IsCompleted && _cache.Result.NeedsToRevalidate())
                    {
                        _cache = Task.Run(() => DoWithDisk(new StaionCollectionRequest {LastRun = _cache.Result}));
                    }
                    return _cache;
                }
                return Task.Run(() => DoWithDisk(new StaionCollectionRequest()));
            }
        }


        private StationCollection DoWithDisk(StaionCollectionRequest staionCollectionRequest)
        {
            string path = Path.GetTempPath() + "Weather\\";
            Directory.CreateDirectory(path);
            path += "StationData.dat";
            StationCollection stationCollection;
            if (File.Exists(path))
            {
                using (FileStream fileStream = File.OpenRead(path))
                {
                    stationCollection = Serializer.Deserialize<StationCollection>(fileStream);
                }
                if (!stationCollection.NeedsToRevalidate())
                {
                    return stationCollection;
                }
                staionCollectionRequest.LastRun = stationCollection;
            }

            stationCollection = Do(staionCollectionRequest);

            using (FileStream fileStream = File.Create(path))
            {
                Serializer.Serialize(fileStream, stationCollection);
            }

            return stationCollection;
        }

        private StationCollection Do(StaionCollectionRequest staionCollectionRequest)
        {
            HttpWebRequest stationRequest =
                WebRequest.CreateHttp("http://www1.ncdc.noaa.gov/pub/data/noaa/ish-history.csv");

            if (staionCollectionRequest.LastRun != null)
            {
                stationRequest.Headers.Add(HttpRequestHeader.CacheControl, "max-age=0");
                if (!String.IsNullOrWhiteSpace(staionCollectionRequest.LastRun.ETag))
                    stationRequest.Headers.Add(HttpRequestHeader.IfNoneMatch, staionCollectionRequest.LastRun.ETag);
                stationRequest.IfModifiedSince = (staionCollectionRequest.LastRun.LastUpdated ??
                                                  staionCollectionRequest.LastRun.RunTime);
            }

            var collection = new StationCollection {RunTime = DateTime.Now};
            var list = new List<Station>();

            try
            {
                using (WebResponse webResponse = stationRequest.GetResponse())
                using (Stream stream = webResponse.GetResponseStream())
                    if (stream != null)
                        using (var reader = new StreamReader(stream))
                        {
                            string stationLine;
                            bool hasSeenHeader = false;
                            while ((stationLine = reader.ReadLine()) != null)
                            {
                                if (hasSeenHeader)
                                {
                                    Station station = ParseStation(stationLine);
                                    if (station != default(Station)) list.Add(station);
                                }
                                else if (stationLine ==
                                         @"""USAF"",""WBAN"",""STATION NAME"",""CTRY"",""FIPS"",""STATE"",""CALL"",""LAT"",""LON"",""ELEV(.1M)"",""BEGIN"",""END""")
                                {
                                    hasSeenHeader = true;
                                }
                                else
                                {
                                    throw new NotSupportedException("station csv bad");
                                }
                            }

                            collection.ETag = webResponse.Headers["ETag"];
                            DateTime tempDateTime;
                            if (DateTime.TryParse(webResponse.Headers["Last-Modified"], out tempDateTime))
                                collection.LastUpdated = tempDateTime;
                        }
            }
            catch (WebException e)
            {
                HttpStatusCode httpStatusCode = ((HttpWebResponse) e.Response).StatusCode;
                if (staionCollectionRequest.LastRun != null && httpStatusCode == HttpStatusCode.NotModified)
                {
                    collection = staionCollectionRequest.LastRun;
                    collection.RunTime = DateTime.Now;
                    return collection;
                }
                WebExceptionStatus webExceptionStatus = e.Status;
            }
// ReSharper disable EmptyGeneralCatchClause
            catch (Exception e)
// ReSharper restore EmptyGeneralCatchClause
            {}

            collection.Stations = list.ToArray();

            return collection;
        }

        private Station ParseStation(string line)
        {
            if (!line.StartsWith("\"") || !line.EndsWith("\""))
                throw new NotSupportedException("station csv bad");

            line = line.Substring(1, line.Length - 2);

            string[] lineElements = line.Split(new[] {"\",\""}, StringSplitOptions.None);

            if (lineElements.Length != 12)
                throw new NotSupportedException("station csv bad");

            float tempFloat;
            var station = new Station
            {
                Identifier = new StationIdentifier
                {
                    UsafId = int.Parse(lineElements[0]),
                    WbanId = int.Parse(lineElements[1])
                },
                Name = lineElements[2],
                Country = lineElements[3],
                FIPSCountryId = lineElements[4],
                State = lineElements[5],
                CallSign = lineElements[6],
                Begin = ParseISO8601DashlessDate(lineElements[10]),
                End = ParseISO8601DashlessDate(lineElements[11])
            };

            if (station.FIPSCountryId != "US") return default(Station);

            if (float.TryParse(lineElements[7], out tempFloat)) station.Latitude = tempFloat/1000;
            else return default(Station);
            if (float.TryParse(lineElements[8], out tempFloat)) station.Longitude = tempFloat/1000;
            else return default(Station);
            if (float.TryParse(lineElements[9], out tempFloat)) station.Elevation = tempFloat/10;

// ReSharper disable CompareOfFloatsByEqualityOperator
            if (station.Latitude == 0 || station.Longitude == 0) return default(Station);
// ReSharper restore CompareOfFloatsByEqualityOperator

            return station;
        }

        private DateTime? ParseISO8601DashlessDate(string s)
        {
            DateTime? date = null;
            int year, month, day;
            if (s != null
                && s.Length == 8
                && int.TryParse(s.Substring(0, 4), out year)
                && int.TryParse(s.Substring(4, 2), out month)
                && int.TryParse(s.Substring(6, 2), out day))
            {
                date = new DateTime(year, month, day);
            }
            return date;
        }
    }

    public class StaionCollectionRequest
    {
        public StationCollection LastRun { get; set; }
    }
}