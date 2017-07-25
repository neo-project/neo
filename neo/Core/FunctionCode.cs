using Neo.IO;
using Neo.IO.Json;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    public class FunctionCode : ICode, ISerializable
    {
        public byte[] Script { get; set; }
        public ContractParameterType[] ParameterList { get; set; }
        public ContractParameterType ReturnType { get; set; }

        private UInt160 _scriptHash;
        public UInt160 ScriptHash
        {
            get
            {
                if (_scriptHash == null)
                {
                    _scriptHash = Script.ToScriptHash();
                }
                return _scriptHash;
            }
        }

        public int Size => Script.GetVarSize() + ParameterList.GetVarSize() + sizeof(ContractParameterType);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Script = reader.ReadVarBytes();
            ParameterList = reader.ReadVarBytes().Select(p => (ContractParameterType)p).ToArray();
            ReturnType = (ContractParameterType)reader.ReadByte();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
            writer.WriteVarBytes(ParameterList.Cast<byte>().ToArray());
            writer.Write((byte)ReturnType);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = ScriptHash.ToString();
            json["script"] = Script.ToHexString();
            json["parameters"] = new JArray(ParameterList.Select(p => (JObject)p));
            json["returntype"] = ReturnType;
            return json;
        }
    }
}
