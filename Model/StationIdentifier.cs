using System;
using ProtoBuf;

namespace Model
{
    [ProtoContract]
    [Serializable]
    public struct StationIdentifier : IComparable<StationIdentifier>, IEquatable<StationIdentifier>
    {
        public bool Equals(StationIdentifier other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return WbanId == other.WbanId && UsafId == other.UsafId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StationIdentifier)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (WbanId * 397) ^ UsafId;
            }
        }

        public static bool operator ==(StationIdentifier left, StationIdentifier right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StationIdentifier left, StationIdentifier right)
        {
            return !Equals(left, right);
        }

        [ProtoMember(1)]
        public int WbanId { get; set; }
        [ProtoMember(2)]
        public int UsafId { get; set; }

        public override string ToString()
        {
            return String.Format("{0:d6}-{1:d5}", UsafId, WbanId);
        }

        public int CompareTo(StationIdentifier other)
        {
            int compareTo;
            if ((compareTo = WbanId.CompareTo(other.WbanId)) != 0) { return compareTo; }

            if ((compareTo = UsafId.CompareTo(other.UsafId)) != 0) { return compareTo; }

            return 0;
        }
    }
}