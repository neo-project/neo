using Neo.Persistence;

namespace Neo.Plugins
{
    public interface IStoragePlugin
    {
        IStore GetStore();
    }
}
