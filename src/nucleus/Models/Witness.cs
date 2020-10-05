using System;
using System.IO;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class Witness : ISerializable
    {
        /// <summary>
        /// This is designed to allow a MultiSig 21/11 (committee)
        /// Invocation = 11 * (64 + 2) = 726
        /// </summary>
        private const int MaxInvocationScript = 1024;

        /// <summary>
        /// Verification = m + (PUSH_PubKey * 21) + length + null + syscall = 1 + ((2 + 33) * 21) + 2 + 1 + 5 = 744
        /// </summary>
        private const int MaxVerificationScript = 1024;

        public byte[] InvocationScript;
        public byte[] VerificationScript;

        public long GasConsumed { get; set; }

        public virtual UInt160 ScriptHash => VerificationScript.ToScriptHash();
        public bool StateDependent => VerificationScript.Length == 0;

        public int Size => InvocationScript.GetVarSize() + VerificationScript.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            InvocationScript = reader.ReadVarBytes(MaxInvocationScript);
            VerificationScript = reader.ReadVarBytes(MaxVerificationScript);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(InvocationScript);
            writer.WriteVarBytes(VerificationScript);
        }

        public JObject ToJson()
        {
            return new JObject()
            {
                ["invocation"] = Convert.ToBase64String(InvocationScript),
                ["verification"] = Convert.ToBase64String(VerificationScript)
            };
        }

        public static Witness FromJson(JObject json)
        {
            return new Witness
            {
                InvocationScript = Convert.FromBase64String(json["invocation"].AsString()),
                VerificationScript = Convert.FromBase64String(json["verification"].AsString())
            };
        }
    }
}
