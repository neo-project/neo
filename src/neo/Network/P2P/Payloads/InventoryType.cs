namespace Neo.Network.P2P.Payloads
{
    public enum InventoryType : byte
    {
        TX = MessageCommand.Transaction,
        Block = MessageCommand.Block,
        Extensible = MessageCommand.Extensible,
        Consensus = MessageCommand.Consensus
    }
}
