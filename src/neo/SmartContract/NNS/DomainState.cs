using Neo.IO;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;
using System.IO;

namespace Neo.SmartContract.NNS
{
    public class DomainState : Nep11TokenState
    {
        public UInt160 Operator { set; get; }
        public uint TimeToLive { set; get; }
        public string Name { set; get; }

        public override int Size => UInt160.Length + sizeof(uint) + Name.GetVarSize();

        public override void FromStackItem(StackItem stackItem){
            Array @array = (Array)stackItem;
            Operator = @array[0].IsNull ? null : @array[1].GetSpan().AsSerializable<UInt160>();
            TimeToLive= (uint)@array[1].GetBigInteger();
            Name = @array[2].IsNull ? null : System.Text.Encoding.UTF8.GetString(@array[2].GetSpan().ToArray());
        }

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Array array = new Array(referenceCounter);
            @array.Add(Operator?.ToArray()?? StackItem.Null);
            @array.Add(TimeToLive);
            @array.Add(Name==null?StackItem.Null:System.Text.Encoding.UTF8.GetBytes(Name));
            return @array;
        }

        public override void Deserialize(BinaryReader reader)
        {
            Operator = reader.ReadSerializable<UInt160>();
            TimeToLive = reader.ReadUInt32();
            Name = reader.ReadVarString(1024);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Operator);
            writer.Write(TimeToLive);
            writer.WriteVarString(Name);
        }
    }
}
