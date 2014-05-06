using System;
using ProtoBuf;

namespace Model
{
    [ProtoContract]
    [Serializable]
    public struct StationYear : IComparable<StationYear>, IEquatable<StationYear>
    {
        public bool Equals(StationYear other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(StationIdentifier, other.StationIdentifier) && Year == other.Year;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StationYear)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((StationIdentifier != null ? StationIdentifier.GetHashCode() : 0) * 397) ^ Year;
            }
        }

        public static bool operator ==(StationYear left, StationYear right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StationYear left, StationYear right)
        {
            return !Equals(left, right);
        }

        [ProtoMember(1)]
        public StationIdentifier StationIdentifier { get; set; }
        [ProtoMember(2)]
        public int Year { get; set; }

        public override string ToString()
        {
            return String.Format("{0}-{1}", StationIdentifier, Year);
        }

        public int CompareTo(StationYear other)
        {
            int compareTo;
            if ((compareTo = StationIdentifier.CompareTo(other.StationIdentifier)) != 0) { return compareTo; }
            if ((compareTo = Year.CompareTo(other.Year)) != 0) { return compareTo; }

            return 0;
        }
    }
}