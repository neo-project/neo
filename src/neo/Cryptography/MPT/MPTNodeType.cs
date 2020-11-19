
namespace Neo.Cryptography.MPT
{
    public enum NodeType : byte
    {
        BranchNode = 0x00,
        ExtensionNode = 0x01,
        LeafNode = 0x02,
        HashNode = 0x03,
        Empty = 0x04
    }
}
