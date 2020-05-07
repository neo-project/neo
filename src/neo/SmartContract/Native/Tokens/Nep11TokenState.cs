using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public class Nep11TokenState : IInteroperable, ISerializable
    {
        public Dictionary<UInt160, BigInteger> owners = new Dictionary<UInt160, BigInteger>();

        public virtual int Size => GetOwnersSize();

        public virtual void FromStackItem(StackItem stackItem)
        {
            Dictionary<UInt160, BigInteger> keyValuePairs = new Dictionary<UInt160, BigInteger>();
            Array array = (Array)((Array)stackItem)[0];
            var key = UInt160.Zero;
            for (int i=0;i<array.Count;i++ )
            {
                if (i % 2 == 0)
                {
                    key = new UInt160(array[i].GetSpan().ToArray());
                    keyValuePairs.Add(new UInt160(array[i].GetSpan().ToArray()), BigInteger.Zero);
                }
                else {
                    keyValuePairs[key] = array[i].GetBigInteger();
                }
            }
            owners = keyValuePairs;
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Array array = new Array(referenceCounter);
            foreach (var v in owners)
            {
                array.Add(new ByteString(v.Key.ToArray()));
                array.Add(v.Value);
            }
            return array;
        }

        public virtual void Deserialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            owners = new System.Collections.Generic.Dictionary<UInt160, System.Numerics.BigInteger>();
            UInt160 key = UInt160.Zero;
            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0)
                {
                    key = reader.ReadSerializable<UInt160>();
                    owners.Add(key, BigInteger.Zero);
                }
                else
                {
                    owners[key] = new BigInteger(reader.ReadVarBytes());
                }
            }
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write(owners.Count);
            foreach (var item in owners)
            {
                writer.Write(item.Key);
                writer.WriteVarBytes(item.Value.ToByteArray());
            }
        }

        public int GetOwnersSize()
        {
            int size = sizeof(int);
            foreach (var item in owners)
            {
                size += item.Key.Size + item.Value.GetVarSize();
            }
            return size;
        }
    }
}
