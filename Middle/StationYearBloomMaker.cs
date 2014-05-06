using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using Model;
using ProtoBuf;

namespace Middle
{
    public sealed class StationYearBloomMaker
    {
        private StationYearBloomMaker(){}

        private static readonly Lazy<StationYearBloomMaker> Lazy = new Lazy<StationYearBloomMaker>(() => new StationYearBloomMaker());
        public static StationYearBloomMaker Instance { get { return Lazy.Value; } }

        private readonly object _lock = new object();

        private Task<StationYearBloomCollection> _task;

        public bool PassFilter(StationYear stationYear)
        {
            GetObservable();
            if (_task.IsCompleted)
            {
                DateTime now = DateTime.Now;
                if (_task.Result.LastUpdated.HasValue)
                {
                    if (_task.Result.LastUpdated.Value.Year < stationYear.Year) return true;
                }
                else
                {
                    if (stationYear.Year == now.Year) return true;
                }
                
                return  _task.Result.StationYearBloom.ContainsKey(stationYear);
            }

            return true;
        }

        public void Prefetch()
        {
            GetObservable();
        }

        private Task<StationYearBloomCollection> GetObservable()
        {
            lock (_lock)
            {
                if (_task != null)
                {
                    if (_task.IsCompleted && _task.Result.NeedsToRevalidate())
                    {
                        _task = Task.Run(() => DoWithDisk(_task.Result));
                    }
                    return _task;
                }
                return _task = Task.Run(() => DoWithDisk(null));
            }
        }

        private StationYearBloomCollection DoWithDisk(StationYearBloomCollection oldCollection)
        {
            string path = Path.GetTempPath() + "Weather\\";
            Directory.CreateDirectory(path);
            path += "StationBloomData.dat";
            StationYearBloomCollection newCollection;
            if (File.Exists(path))
            {
                using (FileStream fileStream = File.OpenRead(path))
                {
                    newCollection = Serializer.Deserialize<StationYearBloomCollection>(fileStream);
                }
                if (!newCollection.NeedsToRevalidate())
                {
                    return newCollection;
                }
                oldCollection = newCollection;
            }

            newCollection = Do(oldCollection);

            using (FileStream fileStream = File.Create(path))
            {
                Serializer.Serialize(fileStream, newCollection);
            }

            return newCollection;
        }

        private StationYearBloomCollection Do(StationYearBloomCollection oldCollection)
        {
            HttpWebRequest stationRequest = WebRequest.CreateHttp("http://www1.ncdc.noaa.gov/pub/data/noaa/ish-inventory.csv.z");

            if (oldCollection != null)
            {
                stationRequest.Headers.Add(HttpRequestHeader.CacheControl, "max-age=0");
                if (!String.IsNullOrWhiteSpace(oldCollection.ETag)) stationRequest.Headers.Add(HttpRequestHeader.IfNoneMatch, oldCollection.ETag);
                stationRequest.IfModifiedSince = (oldCollection.LastUpdated ?? oldCollection.RunTime);
            }

            StationYearBloomCollection collection = new StationYearBloomCollection();
            collection.RunTime = DateTime.Now;
            collection.StationYearBloom = new Dictionary<StationYear, int>();
            try
            {
                using (WebResponse webResponse = stationRequest.GetResponse())
                using (Stream stream = webResponse.GetResponseStream())
                if (stream != null)
                using (var decompress = new GZipStream(stream, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(decompress))
                {
                    string stationLine;
                    bool hasSeenHeader = false;
                    while ((stationLine = reader.ReadLine()) != null)
                    {
                        if (hasSeenHeader)
                        {
                            Tuple<StationYear,int> station = ParseLine(stationLine);
                            if (station != null) collection.StationYearBloom[station.Item1] = station.Item2;
                        }
                        else if (stationLine == @"""USAF"",""WBAN"",""YEAR"",""JAN"",""FEB"",""MAR"",""APR"",""MAY"",""JUN"",""JUL"",""AUG"",""SEP"",""OCT"",""NOV"",""DEC""")
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
                HttpStatusCode httpStatusCode = ((HttpWebResponse)e.Response).StatusCode;
                if (oldCollection != null && httpStatusCode == HttpStatusCode.NotModified)
                {
                    collection = oldCollection;
                    collection.RunTime = DateTime.Now;
                    return collection;
                }
                WebExceptionStatus webExceptionStatus = e.Status;
            }
// ReSharper disable EmptyGeneralCatchClause
            catch (Exception e)
// ReSharper restore EmptyGeneralCatchClause
            {

            }
            
            return collection;
        }

        private Tuple<StationYear, int> ParseLine(string line)
        {
            if (!line.StartsWith("\"") || !line.EndsWith("\""))
                throw new NotSupportedException("station inventory csv bad");

            line = line.Substring(1, line.Length - 2);

            string[] lineElements = line.Split(new[] { "\",\"" }, StringSplitOptions.None);

            if (lineElements.Length != 15)
                throw new NotSupportedException("station inventory csv bad");

            var stationYear = new StationYear
                {
                    StationIdentifier = new StationIdentifier{UsafId = int.Parse(lineElements[0]), WbanId = int.Parse(lineElements[1])},
                    Year = int.Parse(lineElements[2])
                };

            int count = 0;
            for (int i = 3; i < 15; i++)
            {
                count += int.Parse(lineElements[i]);
            }

            return new Tuple<StationYear, int>(stationYear,count);
        }
    }
}
