using Neo.Persistence;

namespace Neo.Network.P2P.Payloads
{
    public interface IInventory : IVerifiable
    {
        UInt256 Hash { get; }

        internal Message OriginalMessage { get; set; }

        InventoryType InventoryType { get; }

        bool Verify(StoreView snapshot);
    }
}
