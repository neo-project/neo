using Neo.Persistence;

namespace Neo.Plugins
{
    public interface IStorageProvider
    {
        IStore GetStore();
    }
}
