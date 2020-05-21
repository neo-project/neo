namespace Neo.Network.P2P.Payloads
{
    public enum InventoryType : byte
    {
        TX = 0x01,
        Block = 0x02,
        StateRoot = 0x03,
        Consensus = 0xe0
    }
}
