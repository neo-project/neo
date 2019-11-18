using System;

namespace Neo.Persistence
{
    public interface ISnapshot : IDisposable, IReadOnlyStore
    {
        void Commit();
        void Delete(byte table, byte[] key);
        void Put(byte table, byte[] key, byte[] value);
    }
}
