using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System.IO;

namespace Neo.Cryptography.MPT
{
    public class ExtensionNode : MPTNode
    {
        //max lenght when store StorageKey
        public const int MaxKeyLength = (InteropService.Storage.MaxKeySize + sizeof(int)) * 2;
        public byte[] Key;
        public MPTNode Next;

        protected override NodeType Type => NodeType.ExtensionNode;

        public override void EncodeSpecific(BinaryWriter writer)
        {
            writer.WriteVarBytes(Key);
            var hashNode = new HashNode(Next.Hash);
            hashNode.EncodeSpecific(writer);
        }

        public override void DecodeSpecific(BinaryReader reader)
        {
            Key = reader.ReadVarBytes(MaxKeyLength);
            var hashNode = new HashNode();
            hashNode.DecodeSpecific(reader);
            Next = hashNode;
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            json["key"] = Key.ToHexString();
            json["next"] = Next.ToJson();
            return json;
        }
    }
}
