using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;

namespace Shared
{
    public static class ConfigHelper
    {
        private static NameValueCollection AppSettings { get { return ConfigurationManager.AppSettings; } }

        public static double DefaultGapFillHours
        {
            get
            {
                double temp;
                if (double.TryParse(AppSettings["DefaultGapFillHours"], out temp) && temp > 0)
                {
                    return temp;
                }
                return 5;
            }
        }

        public static int MaxCachedStationYears
        {
            get
            {
                int temp;
                if (int.TryParse(AppSettings["MaxCachedStationYears"], out temp) && temp > 0)
                {
                    return temp;
                }
                return 5000;
            }
        }

        public static int WorkerCount
        {
            get
            {
                int temp;
                if (int.TryParse(AppSettings["WorkerCount"], out temp) && temp > 0)
                {
                    return temp;
                }
                return 4;
            }
        }
		
        public static double WebCollectionExpireHoursEmpty
        {
            get
            {
                double temp;
                if (double.TryParse(AppSettings["WebCollectionExpireHoursEmpty"], out temp) && temp >= 0)
                {
                    return temp;
                }
                return 168;
            }
        }

        public static double WebCollectionExpireHoursVolatile
        {
            get
            {
                double temp;
                if (double.TryParse(AppSettings["WebCollectionExpireHoursVolatile"], out temp) && temp >= 0)
                {
                    return temp;
                }
                return 6;
            }
        }

        public static double WebCollectionExpireHoursDefault
        {
            get
            {
                double temp;
                if (double.TryParse(AppSettings["WebCollectionExpireHoursDefault"], out temp) && temp >= 0)
                {
                    return temp;
                }
                return 72;
            }
        }
        public static double MaxDaysWeatherRequest
        {
            get
            {
                double temp;
                if (double.TryParse(AppSettings["MaxDaysWeatherRequest"], out temp) && temp >= 0)
                {
                    return temp;
                }
                return 365*6;
            }
        }

        public static IEnumerable<String> GetValidKeys()
        {
            return (AppSettings["ValidApiKeys"] ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
