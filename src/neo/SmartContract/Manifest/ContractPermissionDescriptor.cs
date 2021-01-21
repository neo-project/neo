using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using System;

namespace Neo.SmartContract.Manifest
{
    public class ContractPermissionDescriptor
    {
        public UInt160 Hash { get; }
        public ECPoint Group { get; }

        public bool IsHash => Hash != null;
        public bool IsGroup => Group != null;
        public bool IsWildcard => Hash is null && Group is null;

        private ContractPermissionDescriptor(UInt160 hash, ECPoint group)
        {
            this.Hash = hash;
            this.Group = group;
        }

        internal ContractPermissionDescriptor(ReadOnlySpan<byte> span)
        {
            switch (span.Length)
            {
                case UInt160.Length:
                    Hash = new UInt160(span);
                    break;
                case 33:
                    Group = span.AsSerializable<ECPoint>();
                    break;
                default:
                    throw new ArgumentException(null, nameof(span));
            }
        }

        public static ContractPermissionDescriptor Create(UInt160 hash)
        {
            return new ContractPermissionDescriptor(hash, null);
        }

        public static ContractPermissionDescriptor Create(ECPoint group)
        {
            return new ContractPermissionDescriptor(null, group);
        }

        public static ContractPermissionDescriptor CreateWildcard()
        {
            return new ContractPermissionDescriptor(null, null);
        }

        public static ContractPermissionDescriptor FromJson(JObject json)
        {
            string str = json.AsString();
            if (str.Length == 42)
                return Create(UInt160.Parse(str));
            if (str.Length == 66)
                return Create(ECPoint.Parse(str, ECCurve.Secp256r1));
            if (str == "*")
                return CreateWildcard();
            throw new FormatException();
        }

        public JObject ToJson()
        {
            if (IsHash) return Hash.ToString();
            if (IsGroup) return Group.ToString();
            return "*";
        }
    }
}
