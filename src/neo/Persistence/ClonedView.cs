namespace Neo.Persistence
{
    internal class ClonedView : StoreView
    {
        public override DataCache Storages { get; }

        public ClonedView(StoreView view)
        {
            this.Storages = view.Storages.CreateSnapshot();
        }
    }
}
