namespace Neo.Cryptography.MPT
{
    public enum NodeType : byte
    {
        BranchNode = 0x00,
        ExtensionNode = 0x01,
        HashNode = 0x02,
        LeafNode = 0x03,
    }
}
