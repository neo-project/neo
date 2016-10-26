using AntShares.Core;

namespace AntShares.Network
{
    public interface IInventory : ISignable
    {
        UInt256 Hash { get; }

        InventoryType InventoryType { get; }

        bool Verify();
    }
}
