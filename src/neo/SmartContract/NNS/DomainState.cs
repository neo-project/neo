using Neo.IO;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;
using System.IO;
using System.Numerics;

namespace Neo.SmartContract.NNS
{
    public class DomainState : Nep11TokenState
    {
        public UInt160 Operator { set; get; }
        public uint TimeToLive { set; get; }
        public string Name { set; get; }

        public override int Size => GetOwnersSize()+ UInt160.Length + sizeof(ulong) + Name.GetVarSize();

        public override void FromStackItem(StackItem stackItem){
            base.FromStackItem(stackItem);
            Array @array = (Array)stackItem;
            Operator = @array[1].IsNull ? null : @array[1].GetSpan().AsSerializable<UInt160>();
            TimeToLive= (uint)@array[2].GetBigInteger();
            Name = @array[3].IsNull ? null : System.Text.Encoding.UTF8.GetString(@array[3].GetSpan().ToArray());
        }

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Array @array = (Array)base.ToStackItem(referenceCounter);
            @array.Add(Operator?.ToArray()?? StackItem.Null);
            @array.Add(TimeToLive);
            @array.Add(Name==null?StackItem.Null:System.Text.Encoding.UTF8.GetBytes(Name));
            return @array;
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Operator = reader.ReadSerializable<UInt160>();
            TimeToLive = reader.ReadUInt32();
            Name = reader.ReadVarString(1024);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Operator);
            writer.Write(TimeToLive);
            writer.WriteVarString(Name);
        }
    }
}
