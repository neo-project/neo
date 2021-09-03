using System;
using System.IO;
using System.Linq;
using Neo.Cryptography.ECC;
using Neo.IO;

namespace Neo.Network.P2P.Payloads
{
    public class ContractOrGroup : ISerializable
    {
        private byte[] _data;
        public byte[] Data => _data;
        public int Size => _data?.GetVarSize() ?? 0;
        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(_data);
        }

        public void Deserialize(BinaryReader reader)
        {
            _data = reader.ReadVarBytes();
            if (_data.Length != 33 && _data.Length != 20)
            {
                throw new Exception("Must be UInt160 or PublicKey");
            }
        }

        public static implicit operator ContractOrGroup(UInt160 hash)
        {
            return new ContractOrGroup() { _data = hash.ToArray() };
        }

        public static implicit operator ContractOrGroup(ECPoint point)
        {
            return new ContractOrGroup() { _data = point.EncodePoint(true) };
        }


        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(obj, this)) return true;
            if (obj is ContractOrGroup other)
            {
                if (Data == null || other.Data == null) return Data == other.Data;
                return Data.SequenceEqual(other.Data);
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (Data == null) return 0;
            if (Data.Length >= 4) return BitConverter.ToInt32(Data);
            int hash = Data.Length;

            foreach (var b in Data)
            {
                hash <<= 8;
                hash += b;
            }
            return hash;
        }

        public override string ToString()
        {
            return Data?.ToHexString();
        }

        public string ToHashString()
        {
            if (Data == null) return null;
            if (Data.Length == 20)
            {
                return new UInt160(Data).ToString();
            }
            return Data.ToHexString();
        }
    }
}
