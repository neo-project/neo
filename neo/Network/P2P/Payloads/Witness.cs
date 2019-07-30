using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class Witness : ISerializable
    {
        public WitnessScope Scope;
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
        public bool HasScopedHash => Scope.Type == WitnessScopeType.CustomScriptHash || Scope.Type == WitnessScopeType.ExecutingGroupPubKey;

        public int Size =>
          Scope.Size +
          InvocationScript.GetVarSize() +
          VerificationScript.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Scope = reader.ReadSerializable<WitnessScope>();
            InvocationScript = reader.ReadVarBytes(65536);
            VerificationScript = reader.ReadVarBytes(65536);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Scope);
            writer.WriteVarBytes(InvocationScript);
            writer.WriteVarBytes(VerificationScript);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["scope"] = Scope.ToJson();
            json["invocation"] = InvocationScript.ToHexString();
            json["verification"] = VerificationScript.ToHexString();
            return json;
        }

        public static Witness FromJson(JObject json)
        {
            Witness witness = new Witness();
            witness.Scope = WitnessScope.FromJson(json["scope"]);
            witness.InvocationScript = json["invocation"].AsString().HexToBytes();
            witness.VerificationScript = json["verification"].AsString().HexToBytes();
            return witness;
        }
    }
}
