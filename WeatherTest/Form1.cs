using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Middle;
using Model;

namespace WeatherTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private readonly object _lock = new object();

        private void button1_Click(object sender, EventArgs e)
        {
            Stopwatch full = Stopwatch.StartNew();
            long total = 0;

            //Parallel.ForEach(latLongs.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries).Take(50000), latlong =>
            Parallel.ForEach(new[]{"45.167,-93.833"} , latlong =>
            {
                var parts = latlong.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);
                float lat = float.Parse(parts[0]);
                float lon = float.Parse(parts[1]);


                Stopwatch mines = Stopwatch.StartNew();
                StationYearBloomMaker.Instance.Prefetch();
                StationIdentifier[] stationsIdentifier = StationFinder.Find(lat, lon);


                mines.Stop();

                //MessageBox.Show("start Time " + mines.Elapsed);

                var enumerable = WeatherMerger.Get(stationsIdentifier, DateTime.Now.AddYears(-4),
                    DateTime.Now, TimeSpan.FromHours(24));

                mines = Stopwatch.StartNew();
                var list = enumerable.ToList();
                //MessageBox.Show("end Time " + mines.Elapsed + Environment.NewLine + "end count " + list.Count);
                lock (_lock)
                {
                    total += list.Count;
                }
            });

            full.Stop();
            MessageBox.Show(String.Format("end Time {0}{1}end count {2:N0}", full.Elapsed , Environment.NewLine, total));

            
        }
        #region latLongs

        const string latLongs = @"45.167,-93.833
44.9442,-93.0936"; 
        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            Stopwatch mines = Stopwatch.StartNew();

            var client = new global::WeatherWebServiceClient.Client("https://weather.example.com", "123456");

            var weather = client.GetWeather(44.998889f, -92.909444f, DateTime.Now.AddYears(-7), DateTime.Now, TimeSpan.FromHours(5),50, 100);

            mines.Stop();
            MessageBox.Show(String.Format("end Time {0}{1}end count {2:N0}", mines.Elapsed, Environment.NewLine, weather.Length));

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Stopwatch mines = Stopwatch.StartNew();

            var client = new global::WeatherWebServiceClient.Client("https://weather.example.com", "123456");

            var weather = client.GetAllStations();

            mines.Stop();
            MessageBox.Show(String.Format("end Time {0}{1}end count {2:N0}", mines.Elapsed, Environment.NewLine, weather.Length));
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Stopwatch mines = Stopwatch.StartNew();

            var client = new global::WeatherWebServiceClient.Client("https://weather.example.com", "123456");

            var weather = client.GetStation(new StationIdentifier { UsafId = 690090, WbanId = 99999 });

            mines.Stop();
            MessageBox.Show(String.Format("end Time {0}{1}end count {2}", mines.Elapsed, Environment.NewLine, weather.Identifier));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Stopwatch full = Stopwatch.StartNew();
            long total = 0;

            var client = new global::WeatherWebServiceClient.Client("https://weather.example.com", "123456");

            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = 2;
            options.TaskScheduler = TaskScheduler.Default;

            Parallel.ForEach(latLongs.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Take(100), options,
                latlong =>
            {
                var parts = latlong.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);
                float lat = float.Parse(parts[0]);
                float lon = float.Parse(parts[1]);

                var enumerable = client.GetWeather(lat, lon, DateTime.Now.AddYears(-4), DateTime.Now, TimeSpan.FromHours(5),50, 100);
                var list = enumerable.ToList();
                lock (_lock)
                {
                    total += list.Count;
                }
            });

            full.Stop();
            MessageBox.Show(String.Format("end Time {0}{1}end count {2:N0}", full.Elapsed , Environment.NewLine, total));
            
        }
    }
}