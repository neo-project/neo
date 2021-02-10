namespace Neo.Network.P2P.Payloads
{
    public interface IInventory : IVerifiable
    {
        InventoryType InventoryType { get; }
    }
}
