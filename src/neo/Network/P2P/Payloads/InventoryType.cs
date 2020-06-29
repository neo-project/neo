namespace Neo.Network.P2P.Payloads
{
    public enum InventoryType : byte
    {
        TX = MessageCommand.Transaction,
        Block = MessageCommand.Block,
        StateRoot = MessageCommand.StateRoot,
        Consensus = MessageCommand.Consensus
    }
}
