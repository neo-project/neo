using Neo.Persistence;

namespace Neo.Plugins
{
    public interface IPersistencePlugin
    {
        void OnPersist(Snapshot snapshot);
    }
}
