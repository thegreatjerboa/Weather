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
using WeatherWebService.Attribute;

namespace WeatherWebService.Controllers
{
    public class StationController : Controller
    {
        [HttpGet]
        [AuthenticateFilter]
        public ActionResult GetAll()
        {
            var stations = StationMaker.Instance.Get();

            byte[] data;

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, stations);
                data = ms.ToArray();
            }

            Response.Filter = new GZipStream(Response.Filter, CompressionLevel.Fastest);

            return this.File(data, "application/gzip");
        }

        [HttpGet]
        [AuthenticateFilter]
        public ActionResult Get(StationIdentifier stationIdentifier)
        {
            var station = StationMaker.Instance.Get(stationIdentifier);

            byte[] data;

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, station);
                data = ms.ToArray();
            }

            Response.Filter = new GZipStream(Response.Filter, CompressionLevel.Fastest);

            return this.File(data, "application/gzip");
        }
    }
}
