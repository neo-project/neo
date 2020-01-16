using System.IO;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Trie.MPT
{
    public class HashNode : MPTNode
    {
        public byte[] Hash;

        public HashNode()
        {
            nType = NodeType.HashNode;
        }   

        public HashNode(byte[] hash)
        {
            nType = NodeType.HashNode;
            Hash = (byte[])hash.Clone();
        }
        
        protected override byte[] calHash()
        {
            return (byte[])Hash.Clone();
        }

        public static HashNode EmptyNode()
        {
            return new HashNode(new byte[]{});
        }

        public bool IsEmptyNode => Hash.Length == 0;

        public override JObject ToJson()
        {
            var json = new JObject();
            if (!this.IsEmptyNode) 
            {
                json["hash"] = Hash.ToHexString();
            }
            return json;
        }
    }
}