// Copyright (C) 2015-2025 The Neo Project.
//
// Nep11BalanceKey.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.VM.Types;
using System;
using System.IO;

namespace Neo.Plugins.Trackers.NEP_11
{
    public class Nep11BalanceKey : IComparable<Nep11BalanceKey>, IEquatable<Nep11BalanceKey>, ISerializable
    {
        public readonly UInt160 UserScriptHash;
        public readonly UInt160 AssetScriptHash;
        public ByteString Token;
        public int Size => UInt160.Length + UInt160.Length + Token.GetVarSize();

        public Nep11BalanceKey() : this(new UInt160(), new UInt160(), ByteString.Empty)
        {
        }

        public Nep11BalanceKey(UInt160 userScriptHash, UInt160 assetScriptHash, ByteString tokenId)
        {
            if (userScriptHash == null || assetScriptHash == null || tokenId == null)
                throw new ArgumentNullException();
            UserScriptHash = userScriptHash;
            AssetScriptHash = assetScriptHash;
            Token = tokenId;
        }

        public int CompareTo(Nep11BalanceKey other)
        {
            if (other is null) return 1;
            if (ReferenceEquals(this, other)) return 0;
            int result = UserScriptHash.CompareTo(other.UserScriptHash);
            if (result != 0) return result;
            result = AssetScriptHash.CompareTo(other.AssetScriptHash);
            if (result != 0) return result;
            return (Token.GetInteger() - other.Token.GetInteger()).Sign;
        }

        public bool Equals(Nep11BalanceKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return UserScriptHash.Equals(other.UserScriptHash) && AssetScriptHash.Equals(AssetScriptHash) && Token.Equals(other.Token);
        }

        public override bool Equals(Object other)
        {
            return other is Nep11BalanceKey otherKey && Equals(otherKey);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UserScriptHash.GetHashCode(), AssetScriptHash.GetHashCode(), Token.GetHashCode());
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(UserScriptHash);
            writer.Write(AssetScriptHash);
            writer.WriteVarBytes(Token.GetSpan());
        }

        public void Deserialize(ref MemoryReader reader)
        {
            ((ISerializable)UserScriptHash).Deserialize(ref reader);
            ((ISerializable)AssetScriptHash).Deserialize(ref reader);
            Token = reader.ReadVarMemory();
        }
    }
}
