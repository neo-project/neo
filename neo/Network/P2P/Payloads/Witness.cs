using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class Witness : ISerializable
    {
        public WitnessScope Scope;
        public UInt160 ScopedHash;
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

        public bool HasScopedHash => Scope == WitnessScope.CustomScriptHash || Scope == WitnessScope.ExecutingGroupPubKey;

        public int Size =>
            sizeof(WitnessScope) +
            (HasScopedHash ? UInt160.Length : 0) +
            InvocationScript.GetVarSize() +
            VerificationScript.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Scope = (WitnessScope)reader.ReadByte();
            ScopedHash = HasScopedHash ? reader.ReadSerializable<UInt160>() : UInt160.Zero;
            InvocationScript = reader.ReadVarBytes(65536);
            VerificationScript = reader.ReadVarBytes(65536);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Scope);
            if (HasScopedHash)
            {
                writer.Write(ScopedHash);
            }
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
