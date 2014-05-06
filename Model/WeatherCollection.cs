using System;
using Model.Interfaces;
using ProtoBuf;

namespace Model
{
    [ProtoContract]
    [Serializable]
    public struct WeatherCollection : IWebCollection, IEquatable<WeatherCollection>
    {
        public bool Equals(WeatherCollection other)
        {
            return Equals(WeatherReading, other.WeatherReading) && RunTime.Equals(other.RunTime) && LastUpdated.Equals(other.LastUpdated) && string.Equals(ETag, other.ETag);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is WeatherCollection && Equals((WeatherCollection) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (WeatherReading != null ? WeatherReading.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ RunTime.GetHashCode();
                hashCode = (hashCode*397) ^ LastUpdated.GetHashCode();
                hashCode = (hashCode*397) ^ (ETag != null ? ETag.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(WeatherCollection left, WeatherCollection right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WeatherCollection left, WeatherCollection right)
        {
            return !left.Equals(right);
        }

        [ProtoMember(1)]
        public WeatherReading[] WeatherReading { get; set; }
        [ProtoMember(2)]
        public DateTime RunTime { get; set; }
        [ProtoMember(3)]
        public DateTime? LastUpdated { get; set; }
        [ProtoMember(4)]
        public string ETag { get; set; }

        public bool IsUninitialized { get { return WeatherReading == null; } }
        public bool IsEmpty { get { return IsUninitialized || WeatherReading.Length == 0; } }
        public bool IsVolatile { get { return !IsEmpty && WeatherReading[0].ReadingTime > DateTime.Now.AddDays(-400);}}

        [ProtoAfterDeserialization]
        // ReSharper disable UnusedMember.Local
        private void AfterDeserilization()
            // ReSharper restore UnusedMember.Local
        {
            if (WeatherReading == null) WeatherReading = new WeatherReading[0];
        }

    }
}