using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class Witness : ISerializable
    {
        public const int MaxInvocationScript = 663;
        public const int MaxVerificationScript = 361;

        public byte[] InvocationScript;
        public byte[] VerificationScript;

        internal long GasConsumed { get; set; }

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

        public bool StateDependent => VerificationScript.Length == 0;

        public int Size => InvocationScript.GetVarSize() + VerificationScript.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            // This is designed to allow a MultiSig 10/10 (around 1003 bytes) ~1024 bytes
            // Invocation = 10 * 64 + 10 = 650 ~ 664  (exact is 653)
            InvocationScript = reader.ReadVarBytes(MaxInvocationScript);
            // Verification = 10 * 33 + 10 = 340 ~ 360   (exact is 351)
            VerificationScript = reader.ReadVarBytes(MaxVerificationScript);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(InvocationScript);
            writer.WriteVarBytes(VerificationScript);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["invocation"] = Convert.ToBase64String(InvocationScript);
            json["verification"] = Convert.ToBase64String(VerificationScript);
            return json;
        }
    }
}
