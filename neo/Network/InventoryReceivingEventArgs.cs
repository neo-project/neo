using System.ComponentModel;

namespace Neo.Network
{
    public class InventoryReceivingEventArgs : CancelEventArgs
    {
        public IInventory Inventory { get; }

        public InventoryReceivingEventArgs(IInventory inventory)
        {
            this.Inventory = inventory;
        }
    }
}
