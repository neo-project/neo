using System;

namespace Neo.Persistence
{
    public interface IStore : IDisposable, IReadOnlyStore
    {
        void Delete(byte table, byte[] key);
        ISnapshot GetSnapshot();
        void Put(byte table, byte[] key, byte[] value);
        void PutSync(byte table, byte[] key, byte[] value);
    }
}
