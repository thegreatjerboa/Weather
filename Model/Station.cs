using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Model
{
    [ProtoContract]
    [Serializable]
    public struct Station : IEquatable<Station>
    {
        public bool Equals(Station other)
        {
            return Identifier.Equals(other.Identifier) && string.Equals(Name, other.Name) && string.Equals(Country, other.Country) && string.Equals(FIPSCountryId, other.FIPSCountryId) && string.Equals(State, other.State) && string.Equals(CallSign, other.CallSign) && Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude) && Elevation.Equals(other.Elevation) && Begin.Equals(other.Begin) && End.Equals(other.End);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Station && Equals((Station) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Identifier.GetHashCode();
                hashCode = (hashCode*397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Country != null ? Country.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (FIPSCountryId != null ? FIPSCountryId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (State != null ? State.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (CallSign != null ? CallSign.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Latitude.GetHashCode();
                hashCode = (hashCode*397) ^ Longitude.GetHashCode();
                hashCode = (hashCode*397) ^ Elevation.GetHashCode();
                hashCode = (hashCode*397) ^ Begin.GetHashCode();
                hashCode = (hashCode*397) ^ End.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Station left, Station right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Station left, Station right)
        {
            return !left.Equals(right);
        }

        [ProtoMember(1)]
        public StationIdentifier Identifier { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public string Country { get; set; }
        [ProtoMember(4)]
        public string FIPSCountryId { get; set; }
        [ProtoMember(5)]
        public string State { get; set; }
        [ProtoMember(6)]
        public string CallSign { get; set; }
        [ProtoMember(7)]
        public float Latitude { get; set; }
        [ProtoMember(8)]
        public float Longitude { get; set; }
        /// <summary>
        /// Meters	
        /// </summary>	
        [ProtoMember(9)]
        public float? Elevation { get; set; }
        [ProtoMember(10)]
        public DateTime? Begin { get; set; }
        [ProtoMember(11)]
        public DateTime? End { get; set; }
    }
}