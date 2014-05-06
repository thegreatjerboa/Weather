using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using Model;
using ProtoBuf;

namespace WeatherWebServiceClient
{
    public class Client
    {
        private readonly string _hostname;
        private readonly string _apiKey;

        public Client(string hostname, string apiKey)
        {
            _hostname = hostname;
            _apiKey = apiKey;
        }

        public StationWeatherReading[] GetWeather(float latitude, float longitude, DateTime start, DateTime end, TimeSpan maxGapSize, int maxNumberOfStations = 50, int maxMiles = 100)
        {
            return MakeProtoRequest<StationWeatherReading[]>(String.Format(
                @"/Weather?latitude={0}&longitude={1}&start={2}&end={3}&maxGapSize={4}&maxNumberOfStations={5}&maxMiles={6}"
                , latitude
                , longitude
                , start
                , end
                , maxGapSize
                , maxNumberOfStations
                , maxMiles));
        }

        public Station[] GetAllStations()
        {
            return MakeProtoRequest<Station[]>(@"/Station/GetAll");
        }

        public Station GetStation(StationIdentifier stationIdentifier)
        {
            return MakeProtoRequest<Station>(
                String.Format(@"/Station/Get?stationIdentifier.UsafId={0}&stationIdentifier.WbanId={1}"
                , stationIdentifier.UsafId
                , stationIdentifier.WbanId));
        }

        private T MakeProtoRequest<T>(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(_hostname + url);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(new Cookie("APIKEY", _apiKey, "/", request.RequestUri.Host));
            request.ConnectionGroupName = (DateTime.Now.Ticks/TimeSpan.TicksPerMinute).ToString();

            using (WebResponse webResponse = request.GetResponse())
            using (Stream stream = webResponse.GetResponseStream())
            if (stream != null)
            using (var decompress = new GZipStream(stream, CompressionMode.Decompress))
            {
                return Serializer.Deserialize<T>(decompress);
            }

            throw new InvalidOperationException("Could not do the stuff");
        }
    }
}
