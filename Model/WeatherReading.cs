using System;
using ProtoBuf;

namespace Model
{
    [ProtoContract]
    [Serializable]
    public struct WeatherReading : IComparable<WeatherReading>, IEquatable<WeatherReading>
    {
        public bool Equals(WeatherReading other)
        {
            return ReadingTime.Equals(other.ReadingTime) && Temp.Equals(other.Temp);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is WeatherReading && Equals((WeatherReading) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ReadingTime.GetHashCode()*397) ^ Temp.GetHashCode();
            }
        }

        public static bool operator ==(WeatherReading left, WeatherReading right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WeatherReading left, WeatherReading right)
        {
            return !left.Equals(right);
        }

        [ProtoMember(1)]
        public DateTime ReadingTime { get; set; }
        [ProtoMember(2)]
        public float Temp { get; set; }

        public int CompareTo(WeatherReading other)
        {
            return ReadingTime.CompareTo(other.ReadingTime);
        }
        public override string ToString()
        {
            return String.Format(@"{0} {1}°F", ReadingTime, Temp);
        }
    }

    [ProtoContract]
    [Serializable]
    public struct StationWeatherReading
    {
        [ProtoMember(1)]
        public StationIdentifier Station { get; set; }
        [ProtoMember(2)]
        public WeatherReading WeatherReading { get; set; }

        public static explicit operator WeatherReading(StationWeatherReading stationWeatherReading)
        {
            return stationWeatherReading.WeatherReading;
        }
        public override string ToString()
        {
            return string.Format("{0} {1}", Station, WeatherReading);
        }
    }

    public static class WeatherExtentions
    {
        /// <summary>
        /// Very fast way to sort a nearly sorted collection
        /// </summary>
        public static void EnusureSorted(this WeatherReading[] collection)
        {
            int n = collection.Length;

            for (int outer = 1; outer < n; outer++)
            {
                WeatherReading temp = collection[outer];
                int inner = outer;
                bool didSomeThing = false;
                while (inner > 0 && temp.CompareTo(collection[inner - 1]) < 0)
                {
                    didSomeThing = true;
                    collection[inner] = collection[inner - 1];
                    inner -= 1;
                }
                if (didSomeThing) collection[inner] = temp;
            }
        }
    }
}
