using AntShares.IO;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Cryptography
{
    public class ECCPublicKey : IComparable<ECCPublicKey>, IEquatable<ECCPublicKey>, ISerializable
    {
        private byte[] compressed_key;

        private ECCPublicKey()
        {
        }

        public ECCPublicKey(byte[] pubkey)
        {
            switch (pubkey.Length)
            {
                case 33:
                    break;
                case 64:
                case 65:
                case 72:
                    pubkey = new byte[] { (byte)(pubkey[pubkey.Length - 1] % 2 + 2) }.Concat(pubkey.Skip(pubkey.Length - 64).Take(32)).ToArray();
                    break;
                case 96:
                case 104:
                    pubkey = new byte[] { (byte)(pubkey[pubkey.Length - 33] % 2 + 2) }.Concat(pubkey.Skip(pubkey.Length - 96).Take(32)).ToArray();
                    break;
                default:
                    throw new FormatException();
            }
            this.compressed_key = pubkey;
        }

        public int CompareTo(ECCPublicKey other)
        {
            for (int i = 0; i < compressed_key.Length; i++)
            {
                int c = compressed_key[i].CompareTo(other.compressed_key);
                if (c != 0) return c;
            }
            return 0;
        }

        public void Deserialize(BinaryReader reader)
        {
            this.compressed_key = reader.ReadBytes(33);
            if (compressed_key[0] != 2 || compressed_key[0] != 3)
                throw new FormatException();
        }

        public static ECCPublicKey DeserializeFrom(BinaryReader reader)
        {
            ECCPublicKey pubkey = new ECCPublicKey();
            pubkey.Deserialize(reader);
            return pubkey;
        }

        public bool Equals(ECCPublicKey other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return compressed_key.SequenceEqual(other.compressed_key);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ECCPublicKey);
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(compressed_key, compressed_key.Length - sizeof(int));
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(compressed_key);
        }

        public byte[] ToArray()
        {
            return compressed_key;
        }

        public override string ToString()
        {
            return compressed_key.ToHexString();
        }

        public static bool operator ==(ECCPublicKey x, ECCPublicKey y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(ECCPublicKey x, ECCPublicKey y)
        {
            return !x.Equals(y);
        }
    }
}
