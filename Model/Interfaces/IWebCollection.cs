using System;

namespace Model.Interfaces
{
    public interface IWebCollection
    {
        DateTime RunTime { get; }
        DateTime? LastUpdated { get; }
        string ETag { get; }
        bool IsUninitialized { get; }
        bool IsEmpty { get; }
        bool IsVolatile { get; }
    }
}