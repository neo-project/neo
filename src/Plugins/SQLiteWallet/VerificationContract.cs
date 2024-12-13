// Copyright (C) 2015-2024 The Neo Project.
//
// VerificationContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.SmartContract;

namespace Neo.Wallets.SQLite
{
    class VerificationContract : SmartContract.Contract, IEquatable<VerificationContract>, ISerializable
    {
        public int Size => ParameterList.GetVarSize() + Script.GetVarSize();

        public void Deserialize(ref MemoryReader reader)
        {
            ReadOnlySpan<byte> span = reader.ReadVarMemory().Span;
            ParameterList = new ContractParameterType[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                ParameterList[i] = (ContractParameterType)span[i];
                if (!Enum.IsDefined(typeof(ContractParameterType), ParameterList[i]))
                    throw new FormatException();
            }
            Script = reader.ReadVarMemory().ToArray();
        }

        public bool Equals(VerificationContract other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return ScriptHash.Equals(other.ScriptHash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as VerificationContract);
        }

        public override int GetHashCode()
        {
            return ScriptHash.GetHashCode();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(ParameterList.Select(p => (byte)p).ToArray());
            writer.WriteVarBytes(Script);
        }
    }
}
