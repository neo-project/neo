using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.Cryptography.MPT
{
    public class LeafNode : MPTNode
    {
        //the max size when store StorageItem
        public const int MaxValueLength = 3 + InteropService.Storage.MaxValueSize + sizeof(bool);
        public byte[] Value;

        protected override NodeType Type => NodeType.LeafNode;

        public LeafNode()
        {
        }

        public LeafNode(ReadOnlySpan<byte> val)
        {
            Value = val.ToArray();
        }

        public override void EncodeSpecific(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
        }

        public override void DecodeSpecific(BinaryReader reader)
        {
            Value = reader.ReadVarBytes(MaxValueLength);
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            json["value"] = Value.ToHexString();
            return json;
        }
    }
}
