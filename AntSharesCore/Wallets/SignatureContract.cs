using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using System.IO;
using System.Linq;

namespace AntShares.Wallets
{
    public class SignatureContract : Contract
    {
        private ECPoint publicKey;

        public static SignatureContract Create(ECPoint publicKey)
        {
            return new SignatureContract
            {
                RedeemScript = CreateSignatureRedeemScript(publicKey),
                PublicKeyHash = publicKey.EncodePoint(true).ToScriptHash(),
                publicKey = publicKey
            };
        }

        public static byte[] CreateSignatureRedeemScript(ECPoint publicKey)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.Push(publicKey.EncodePoint(true));
                sb.Add(ScriptOp.OP_CHECKSIG);
                return sb.ToArray();
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            this.publicKey = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            base.RedeemScript = CreateSignatureRedeemScript(publicKey);
            base.PublicKeyHash = publicKey.EncodePoint(true).ToScriptHash();
        }

        public override bool IsCompleted(ECPoint[] publicKeys)
        {
            return publicKeys.Contains(publicKey);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(publicKey);
        }
    }
}
