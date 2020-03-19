namespace Neo.Network.P2P.Payloads
{
    public enum InventoryType : byte
    {
        TX = MessageCommand.Transaction,
        Block = MessageCommand.Block,
        Consensus = MessageCommand.Consensus
    }
}
