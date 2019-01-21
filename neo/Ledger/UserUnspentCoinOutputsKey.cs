using System;
using System.Collections.Generic;
using System.IO;
using Neo.IO;

namespace Neo.Ledger
{
    public class UserUnspentCoinOutputsKey : IComparable<UserUnspentCoinOutputsKey>, IEquatable<UserUnspentCoinOutputsKey>,
        ISerializable
    {
        public bool IsGoverningToken; // It's either the governing token or the utility token
        public readonly UInt160 UserAddress;
        public readonly UInt256 TxHash;

        public int Size => 1 + UserAddress.Size + TxHash.Size;

        public bool Equals(UserUnspentCoinOutputsKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return IsGoverningToken == other.IsGoverningToken && Equals(UserAddress, other.UserAddress) && Equals(TxHash, other.TxHash);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UserUnspentCoinOutputsKey) obj);
        }

        public int CompareTo(UserUnspentCoinOutputsKey other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var isGoverningTokenComparison = IsGoverningToken.CompareTo(other.IsGoverningToken);
            if (isGoverningTokenComparison != 0) return isGoverningTokenComparison;
            var userAddressComparison = Comparer<UInt160>.Default.Compare(UserAddress, other.UserAddress);
            if (userAddressComparison != 0) return userAddressComparison;
            return Comparer<UInt256>.Default.Compare(TxHash, other.TxHash);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = IsGoverningToken.GetHashCode();
                hashCode = (hashCode * 397) ^ (UserAddress != null ? UserAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TxHash != null ? TxHash.GetHashCode() : 0);
                return hashCode;
            }
        }

        public UserUnspentCoinOutputsKey()
        {
            UserAddress = new UInt160();
            TxHash = new UInt256();
        }

        public UserUnspentCoinOutputsKey(bool isGoverningToken, UInt160 userAddress, UInt256 txHash)
        {
            IsGoverningToken = isGoverningToken;
            UserAddress = userAddress;
            TxHash = txHash;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(IsGoverningToken);
            writer.Write(UserAddress.ToArray());
            writer.Write(TxHash.ToArray());
        }

        public void Deserialize(BinaryReader reader)
        {
            IsGoverningToken = reader.ReadBoolean();
            ((ISerializable) UserAddress).Deserialize(reader);
            ((ISerializable) TxHash).Deserialize(reader);
        }
    }
}