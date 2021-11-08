// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers.Binary;
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
            if (hash is null) throw new ArgumentNullException();
            return new ContractOrGroup() { _data = hash.ToArray() };
        }

        public static implicit operator ContractOrGroup(ECPoint point)
        {
            if (point is null) throw new ArgumentNullException();
            return new ContractOrGroup() { _data = point.EncodePoint(true) };
        }


        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(obj, this)) return true;
            if (obj is ContractOrGroup other)
            {
                if (Data is null || other.Data is null) return Data == other.Data;
                return Data.SequenceEqual(other.Data);
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (Data == null) return 0;
            if (Data.Length >= 4) return BinaryPrimitives.ReadInt32LittleEndian(Data);
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
            if (_data == null) return null;
            if (_data.Length == 20)
            {
                return new UInt160(_data).ToString();
            }
            return _data.ToHexString();
        }
    }
}
