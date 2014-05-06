using System;
using Model.Interfaces;
using Shared;

namespace Middle
{
    public static class IWebCollectionExtentions
    {
        //public static readonly Random Random = new Random();

        public static bool NeedsToRevalidate(this IWebCollection collection)
        {
            //int next = Random.Next(0,999999999);
            //if (next%1000 == 0) return true;

            if (collection.IsUninitialized) return true;

            TimeSpan actualDifference = DateTime.Now.Subtract(collection.RunTime);

            if (collection.IsEmpty)
            {
                return actualDifference > TimeSpan.FromHours(ConfigHelper.WebCollectionExpireHoursEmpty);
            }
            if (collection.IsVolatile)
            {
                return actualDifference > TimeSpan.FromHours(ConfigHelper.WebCollectionExpireHoursVolatile);
            }

            return actualDifference > TimeSpan.FromHours(ConfigHelper.WebCollectionExpireHoursDefault);
        }
    }
}
