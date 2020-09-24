using System;
using System.IO;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class Witness : ISerializable
    {
        public byte[] InvocationScript;
        public byte[] VerificationScript;

        public long GasConsumed { get; set; }

        public virtual UInt160 ScriptHash => VerificationScript.ToScriptHash();
        public bool StateDependent => VerificationScript.Length == 0;

        public int Size => InvocationScript.GetVarSize() + VerificationScript.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            // This is designed to allow a MultiSig 10/10 (around 1003 bytes) ~1024 bytes
            // Invocation = 10 * 64 + 10 = 650 ~ 664  (exact is 653)
            InvocationScript = reader.ReadVarBytes(663);
            // Verification = 10 * 33 + 10 = 340 ~ 360   (exact is 351)
            VerificationScript = reader.ReadVarBytes(361);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(InvocationScript);
            writer.WriteVarBytes(VerificationScript);
        }
    }
}
