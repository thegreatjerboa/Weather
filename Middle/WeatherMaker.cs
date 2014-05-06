using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Model;
using ProtoBuf;
using Shared;

namespace Middle
{
    public sealed class WeatherMaker
    {
        private readonly int _workerCount = ConfigHelper.WorkerCount;
        private readonly List<Thread> _workers;
        private readonly int _maxStationYears = ConfigHelper.MaxCachedStationYears;

        private WeatherMaker()
        {
            _workers = new List<Thread>();
            foreach (int i in Enumerable.Range(0, _workerCount))
            {
                var worker = new Thread(WorkerLoop) {IsBackground = true, Priority = ThreadPriority.BelowNormal};
                worker.Start();
                _workers.Add(worker);
            }
        }

        private static readonly Lazy<WeatherMaker> Lazy = new Lazy<WeatherMaker>(() => new WeatherMaker());

        public static WeatherMaker Instance { get { return Lazy.Value; } }

        private readonly Dictionary<StationYear, TaskCompletionSource<WeatherCollection>> _cache = new Dictionary<StationYear, TaskCompletionSource<WeatherCollection>>();

        private readonly SortedSet<StationYearRequest> _queue = new SortedSet<StationYearRequest>();

        private readonly Dictionary<StationYear, StationYearRequest> _queueIndex = new Dictionary<StationYear, StationYearRequest>();

        private readonly object _collectionLock = new object();

        private readonly AutoResetEvent _readyEvent = new AutoResetEvent(false);

        public void Prefetch(StationYear stationYear, int priority)
        {
            GetObservable(stationYear, priority);
        }

        private Task<WeatherCollection> GetObservable(StationYear stationYear, int priority)
        {
            Task<WeatherCollection> retval;

            lock (_collectionLock)
            {
                StationYearRequest stationYearRequest;
                TaskCompletionSource<WeatherCollection> weatherCollection;
                if (_cache.TryGetValue(stationYear, out weatherCollection))
                {
                    retval = weatherCollection.Task;

                    if (retval.IsCompleted && retval.Result.NeedsToRevalidate())
                    {
                        stationYearRequest = new StationYearRequest();
                        stationYearRequest.Priority = priority;
                        stationYearRequest.RequestTime = DateTime.Now;
                        stationYearRequest.StationYear = stationYear;
                        stationYearRequest.LastRun = retval.Result;

                        var subject = new TaskCompletionSource<WeatherCollection>();

                        _cache[stationYear] = subject;
                        _queue.Add(stationYearRequest);
                        _queueIndex[stationYear] = stationYearRequest;

                        retval = subject.Task;
                    }
                    else
                    {
                        if (_queueIndex.TryGetValue(stationYear, out stationYearRequest) &&
                            stationYearRequest.Priority > priority)
                        {
                            _queue.Remove(stationYearRequest);
                            stationYearRequest.Priority = priority;
                            _queue.Add(stationYearRequest);
                            _queueIndex[stationYear] = stationYearRequest;
                        }
                    }

                }
                else
                {
                    stationYearRequest = new StationYearRequest();
                    stationYearRequest.Priority = priority;
                    stationYearRequest.RequestTime = DateTime.Now;
                    stationYearRequest.StationYear = stationYear;

                    var subject = new TaskCompletionSource<WeatherCollection>();

                    _cache[stationYear] = subject;
                    _queue.Add(stationYearRequest);
                    _queueIndex[stationYear] = stationYearRequest;

                    retval = subject.Task;
                }
            }
            _readyEvent.Set();
            return retval;
        }

        public WeatherCollection Get(StationYear stationYear)
        {
            return GetObservable(stationYear, -1).Result;
        }

        private WeatherCollection Do(StationYearRequest stationYearRequest)
        {
            var stationYear = stationYearRequest.StationYear;
            string url = String.Format(@"http://www1.ncdc.noaa.gov/pub/data/noaa/isd-lite/{2}/{0:D6}-{1:D5}-{2:D4}.gz"
                                        , stationYear.StationIdentifier.UsafId
                                        , stationYear.StationIdentifier.WbanId
                                        , stationYear.Year);
            var request = (HttpWebRequest)WebRequest.Create(url);
            //request.Method = "HEAD";

            if (stationYearRequest.LastRun != default(WeatherCollection))
            {
                request.Headers.Add(HttpRequestHeader.CacheControl, "max-age=0");
                if (!String.IsNullOrWhiteSpace(stationYearRequest.LastRun.ETag)) request.Headers.Add(HttpRequestHeader.IfNoneMatch, stationYearRequest.LastRun.ETag);
                request.IfModifiedSince = (stationYearRequest.LastRun.LastUpdated ?? stationYearRequest.LastRun.RunTime);
            }

            List<WeatherReading> list = new List<WeatherReading>();
            var collection = new WeatherCollection {RunTime = DateTime.Now};
            try
            {
                using (WebResponse webResponse = request.GetResponse())
                using (Stream stream = webResponse.GetResponseStream())
                if(stream != null)
                using (var decompress = new GZipStream(stream, CompressionMode.Decompress))
                using (var reader = new StreamReader(decompress))
                //using (MemoryStream ms = new MemoryStream())
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        WeatherReading weatherReading = ProcessWeatherLine(line);
                        if (weatherReading != default(WeatherReading)) list.Add(weatherReading);
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
                if (stationYearRequest.LastRun != default(WeatherCollection) && httpStatusCode == HttpStatusCode.NotModified)
                {
                    collection = stationYearRequest.LastRun;
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

            WeatherReading[] weatherReadings = list.ToArray();
            weatherReadings.EnusureSorted();
            collection.WeatherReading = weatherReadings;

            return collection;
        }
        private object _fileLock = new object();

        private WeatherCollection DoWithDisk(StationYearRequest stationYearRequest)
        {
            string path = Path.GetTempPath() + "Weather\\Readings\\";
            Directory.CreateDirectory(path);
            path += stationYearRequest.StationYear + ".dat";
            WeatherCollection weatherCollection;
            if (File.Exists(path))
            {
                try
                {
                    lock (_fileLock)
                    {
                        using (FileStream fileStream = File.OpenRead(path))
                        {
                            weatherCollection = Serializer.Deserialize<WeatherCollection>(fileStream);
                        }
                    }

                    if (!weatherCollection.NeedsToRevalidate())
                    {
                        return weatherCollection;
                    }

                    stationYearRequest.LastRun = weatherCollection;
                }
// ReSharper disable EmptyGeneralCatchClause
                catch (Exception e)
// ReSharper restore EmptyGeneralCatchClause
                {
                }
            }

            weatherCollection = Do(stationYearRequest);

            lock (_fileLock)
            {
                using (FileStream fileStream = File.Create(path))
                {
                    Serializer.Serialize(fileStream, weatherCollection);
                }
            }

            return weatherCollection;
        }

        private WeatherReading ProcessWeatherLine(string line)
        {
            WeatherReading weatherReading = default(WeatherReading);
            DateTime date = new DateTime(int.Parse(line.Substring(0, 4)), int.Parse(line.Substring(5, 2)), int.Parse(line.Substring(8, 2)), int.Parse(line.Substring(11, 2)), 0, 0);

            //Parsed Values
            float tempFloat;
            float? cTenthsTemp =
                Single.TryParse(line.Substring(13, 6), out tempFloat) && tempFloat > -9000
                ? tempFloat
                : (float?)null;

            if (cTenthsTemp.HasValue)
            {
                weatherReading = new WeatherReading {ReadingTime = date, Temp = ((cTenthsTemp.Value/10.0f)*1.8f) + 32};
            }

            return weatherReading;
        }

        private void WorkerLoop()
        {

            StationYearRequest item = null;
            bool ready = false;
            while (true)                        // Keep consuming until
            {                                   // told otherwise.

                while (!ready)
                {
                    _readyEvent.WaitOne(300);
                    lock (_collectionLock)
                    {
                        item = _queue.Min;
                        if (item != null)
                        {
                            ready = true;
                            _queueIndex.Remove(item.StationYear);
                            _queue.Remove(item);
                        }
                    }
                }

                WeatherCollection weatherCollection = StationYearBloomMaker.Instance.PassFilter(item.StationYear)
                    ? DoWithDisk(item) 
                    : new WeatherCollection { RunTime = DateTime.Now, WeatherReading = new WeatherReading[0] };

                lock (_collectionLock)
                {
                    _cache[item.StationYear].SetResult(weatherCollection);

                    if (_cache.Count > _maxStationYears)
                    {
                        var pairs = _cache.Where(kvp => kvp.Value.Task.IsCompleted).ToArray();
                        if (pairs.Length > _maxStationYears)
                        {
                            pairs = pairs.OrderByDescending(kvp => kvp.Value.Task.Result.LastUpdated)
                                .Take(_maxStationYears /2).ToArray();
                            foreach (var pair in pairs)
                            {
                                _cache.Remove(pair.Key);
                            }
                        }
                    }

                    item = _queue.Min;
                    if (item != null)
                    {
                        ready = true;
                        _queueIndex.Remove(item.StationYear);
                        _queue.Remove(item);
                    }
                    else
                    {
                        ready = false;
                    }
                }
            }
        }
    }
}
