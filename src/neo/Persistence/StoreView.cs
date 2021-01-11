namespace Neo.Persistence
{
    /// <summary>
    /// It provides a set of properties and methods for reading formatted data from the underlying storage. Such as <see cref="Blocks"/> and <see cref="Transactions"/>.
    /// </summary>
    public abstract class StoreView
    {
        public abstract DataCache Storages { get; }

        public StoreView Clone()
        {
            return new ClonedView(this);
        }

        public virtual void Commit()
        {
            Storages.Commit();
        }
    }
}
