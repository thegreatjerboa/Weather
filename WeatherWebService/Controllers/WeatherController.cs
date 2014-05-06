using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Middle;
using Model;
using ProtoBuf;
using Shared;
using WeatherWebService.Attribute;

namespace WeatherWebService.Controllers
{
    public class WeatherController : Controller
    {
        //[HttpGet]
        //public ActionResult Index(float latitude, float longitude, DateTime start, DateTime end, int maxNumberOfStations = 50, int maxMiles = 100)
        //{
        //    return Index(latitude, longitude, start, end,TimeSpan.FromHours(5), maxNumberOfStations, maxMiles);
        //}

        [HttpGet]
        [AuthenticateFilter]
        public ActionResult Index(float latitude, float longitude, DateTime start, DateTime end, TimeSpan maxGapSize, int maxNumberOfStations = 50, int maxMiles = 100)
        {
            StationYearBloomMaker.Instance.Prefetch();
            StationIdentifier[] stationsIdentifier = StationFinder.Find(latitude, longitude,maxNumberOfStations,maxMiles);

            if (start > end) return new EmptyResult();

            TimeSpan maxRequest = TimeSpan.FromDays(ConfigHelper.MaxDaysWeatherRequest);
            if (end.Subtract(start) < maxRequest) start = end.Subtract(maxRequest);
            
            StationWeatherReading[] stationWeather = WeatherMerger.Get(stationsIdentifier, start, end, maxGapSize).ToArray();

            byte[] data;

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, stationWeather);
                data = ms.ToArray();
            }
            Response.Filter = new GZipStream(Response.Filter, CompressionLevel.Fastest);

            return this.File(data, "application/gzip");
        }

    }
}
