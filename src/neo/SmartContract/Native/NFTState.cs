using Neo.IO;
using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;

namespace Neo.SmartContract.Native
{
    public abstract class NFTState : IInteroperable
    {
        public UInt160 Owner;
        public string Name;
        public string Description;

        public abstract byte[] Id { get; }

        public virtual JObject ToJson()
        {
            return new JObject
            {
                ["name"] = Name,
                ["description"] = Description
            };
        }

        public virtual void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Owner = new UInt160(@struct[0].GetSpan());
            Name = @struct[1].GetString();
            Description = @struct[2].GetString();
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { Owner.ToArray(), Name, Description };
        }
    }
}
