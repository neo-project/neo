using Neo.IO.Caching;

namespace Neo.Trie.MPT
{
    public enum NodeType : byte
    {
        [ReflectionCache(typeof(BranchNode))]
        BranchNode = 0x00,

        [ReflectionCache(typeof(ExtensionNode))]
        ExtensionNode = 0x01,

        [ReflectionCache(typeof(HashNode))]
        HashNode = 0x02,

        [ReflectionCache(typeof(LeafNode))]
        LeafNode = 0x03,
    }
}
