using AntShares.Cryptography.ECC;
using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    public class ValidatorState : ISerializable
    {
        public const byte StateVersion = 0;
        public ECPoint PublicKey;

        int ISerializable.Size => sizeof(byte) + PublicKey.Size;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != StateVersion) throw new FormatException();
            PublicKey = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(StateVersion);
            writer.Write(PublicKey);
        }
    }
}
