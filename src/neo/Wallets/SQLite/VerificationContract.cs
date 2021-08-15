// Copyright (C) 2015-2021 NEO GLOBAL DEVELOPMENT.
// 
// The Neo project is free software distributed under the MIT software license, 
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

        public void Deserialize(BinaryReader reader)
        {
            ParameterList = reader.ReadVarBytes().Select(p =>
            {
                var ret = (ContractParameterType)p;
                if (!Enum.IsDefined(typeof(ContractParameterType), ret))
                    throw new FormatException();
                return ret;
            }).ToArray();
            Script = reader.ReadVarBytes();
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
