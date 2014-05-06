using System;
using Model.Interfaces;
using ProtoBuf;

namespace Model
{
    [ProtoContract]
    [Serializable]
    public class StationCollection : IWebCollection
    {
        [ProtoMember(1)]
        public Station[] Stations { get; set; }
        [ProtoMember(2)]
        public DateTime RunTime { get; set; }
        [ProtoMember(3)]
        public DateTime? LastUpdated { get; set; }
        [ProtoMember(4)]
        public string ETag { get; set; }

        public bool IsUninitialized { get { return Stations == null; } }
        public bool IsEmpty { get { return IsUninitialized || Stations.Length == 0; } }
        public bool IsVolatile {get { return true; }}

        [ProtoAfterDeserialization]
        // ReSharper disable UnusedMember.Local
        private void AfterDeserilization()
            // ReSharper restore UnusedMember.Local
        {
            if (Stations == null) Stations = new Station[0];
        }

    }
}