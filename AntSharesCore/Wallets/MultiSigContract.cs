using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Wallets
{
    public class MultiSigContract : Contract
    {
        private int m;
        private ECPoint[] publicKeys;

        public static MultiSigContract Create(UInt160 publicKeyHash, int m, params ECPoint[] publicKeys)
        {
            return new MultiSigContract
            {
                RedeemScript = CreateMultiSigRedeemScript(m, publicKeys),
                PublicKeyHash = publicKeyHash,
                m = m,
                publicKeys = publicKeys
            };
        }

        public static byte[] CreateMultiSigRedeemScript(int m, params ECPoint[] publicKeys)
        {
            if (!(1 <= m && m <= publicKeys.Length && publicKeys.Length <= 1024))
                throw new ArgumentException();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.Push(m);
                foreach (ECPoint publicKey in publicKeys.OrderBy(p => p))
                {
                    sb.Push(publicKey.EncodePoint(true));
                }
                sb.Push(publicKeys.Length);
                sb.Add(ScriptOp.OP_CHECKMULTISIG);
                return sb.ToArray();
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            m = (int)reader.ReadVarInt(int.MaxValue);
            publicKeys = new ECPoint[reader.ReadVarInt(0x10000000)];
            for (int i = 0; i < publicKeys.Length; i++)
            {
                publicKeys[i] = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            }
            RedeemScript = CreateMultiSigRedeemScript(m, publicKeys);
            PublicKeyHash = reader.ReadSerializable<UInt160>();
        }

        public override bool IsCompleted(ECPoint[] publicKeys)
        {
            return publicKeys.Count(p => this.publicKeys.Contains(p)) >= m;
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.WriteVarInt(m);
            writer.Write(publicKeys);
            writer.Write(PublicKeyHash);
        }
    }
}
