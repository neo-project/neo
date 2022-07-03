// Copyright (C) 2015-2022 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.SmartContract;
using System;
using System.IO;
using System.Linq;

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
            return other is not null && ScriptHash.Equals(other.ScriptHash);
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
