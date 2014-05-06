using System;

namespace Model
{
    public class StationYearRequest : IComparable<StationYearRequest>, IEquatable<StationYearRequest>
    {
        public bool Equals(StationYearRequest other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(StationYear, other.StationYear) && Priority == other.Priority && RequestTime.Equals(other.RequestTime) && Equals(LastRun, other.LastRun);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StationYearRequest)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StationYear.GetHashCode();//(StationYear != null ? StationYear.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Priority;
                hashCode = (hashCode * 397) ^ RequestTime.GetHashCode();
                hashCode = (hashCode * 397) ^ LastRun.GetHashCode();//(LastRun != null ? LastRun.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(StationYearRequest left, StationYearRequest right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StationYearRequest left, StationYearRequest right)
        {
            return !Equals(left, right);
        }

        public StationYear StationYear { get; set; }
        public int Priority { get; set; }
        public DateTime RequestTime { get; set; }
        public WeatherCollection LastRun { get; set; }


        public override string ToString()
        {
            return String.Format("{0} {1}", StationYear, Priority);
        }

        public int CompareTo(StationYearRequest other)
        {
            int compareTo;
            if ((compareTo = Priority.CompareTo(other.Priority)) != 0) { return compareTo; }
            if ((compareTo = RequestTime.CompareTo(other.RequestTime)) != 0) { return compareTo; }
            if ((compareTo = StationYear.CompareTo(other.StationYear)) != 0) { return compareTo; }

            return 0;
        }
    }
}