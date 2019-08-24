using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.VM;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class Witness : ISerializable
    {
        public byte[] InvocationScript;
        public byte[] VerificationScript;

        private UInt160 _scriptHash;
        public virtual UInt160 ScriptHash
        {
            get
            {
                if (_scriptHash == null)
                {
                    _scriptHash = VerificationScript.ToScriptHash();
                }
                return _scriptHash;
            }
        }

        public int Size => InvocationScript.GetVarSize() + VerificationScript.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            // This is designed to allow a MultiSig 10/10 (around 1003 bytes) ~1024 bytes
            // Invocation = 10 * 64 + 10 = 650 ~ 664  (exact is 653)
            InvocationScript = reader.ReadVarBytes(664);
            // Verification = 10 * 33 + 10 = 340 ~ 360   (exact is 350)
            VerificationScript = reader.ReadVarBytes(360);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(InvocationScript);
            writer.WriteVarBytes(VerificationScript);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["invocation"] = InvocationScript.ToHexString();
            json["verification"] = VerificationScript.ToHexString();
            return json;
        }

        public static Witness FromJson(JObject json)
        {
            Witness witness = new Witness();
            witness.InvocationScript = json["invocation"].AsString().HexToBytes();
            witness.VerificationScript = json["verification"].AsString().HexToBytes();
            return witness;
        }
    }
}
