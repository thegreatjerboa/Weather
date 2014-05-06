using System;
using System.Collections.Generic;
using System.Linq;
using Model.Interfaces;
using ProtoBuf;

namespace Model
{
    [ProtoContract]
    [Serializable]
    public class StationYearBloomCollection : IWebCollection
    {
        [ProtoMember(1)]
        public Dictionary<StationYear, int> StationYearBloom { get; set; }
        [ProtoMember(2)]
        public DateTime RunTime { get; set; }
        [ProtoMember(3)]
        public DateTime? LastUpdated { get; set; }
        [ProtoMember(4)]
        public string ETag { get; set; }

        public bool IsUninitialized { get { return StationYearBloom == null; } }
        public bool IsEmpty { get { return IsUninitialized || !StationYearBloom.Any(); } }
        public bool IsVolatile { get { return true; } }

        [ProtoAfterDeserialization]
        // ReSharper disable UnusedMember.Local
        private void AfterDeserilization()
            // ReSharper restore UnusedMember.Local
        {
            if (StationYearBloom == null) StationYearBloom = new Dictionary<StationYear, int>();
        }

    }
}