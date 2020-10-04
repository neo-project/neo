using Neo.IO;
using Neo.IO.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// For technical details of ABI, please refer to NEP-3: NeoContract ABI. (https://github.com/neo-project/proposals/blob/master/nep-3.mediawiki)
    /// </summary>
    public class ContractAbi : ISerializable
    {
        /// <summary>
        /// Max length for a valid Contract Abi
        /// </summary>
        public const int MaxLength = 4096;

        private IReadOnlyDictionary<string, ContractMethodDescriptor> methodDictionary;

        /// <summary>
        /// Serialized size
        /// </summary>
        public int Size
        {
            get
            {
                int size = Length;
                return IO.Helper.GetVarSize(size) + size;
            }
        }

        /// <summary>
        /// Length
        /// </summary>
        public int Length => Utility.StrictUTF8.GetByteCount(ToString());

        /// <summary>
        /// Methods is an array of Method objects which describe the details of each method in the contract.
        /// </summary>
        public ContractMethodDescriptor[] Methods { get; set; }

        /// <summary>
        /// Events is an array of Event objects which describe the details of each event in the contract.
        /// </summary>
        public ContractEventDescriptor[] Events { get; set; }

        public ContractAbi Clone()
        {
            return new ContractAbi
            {
                Methods = Methods.Select(p => p.Clone()).ToArray(),
                Events = Events.Select(p => p.Clone()).ToArray()
            };
        }

        /// <summary>
        /// Parse ContractAbi from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractAbi</returns>
        public static ContractAbi FromJson(JObject json)
        {
            var abi = new ContractAbi();
            abi.DeserializeFromJson(json);
            return abi;
        }

        public ContractMethodDescriptor GetMethod(string name)
        {
            methodDictionary ??= Methods.ToDictionary(p => p.Name);
            methodDictionary.TryGetValue(name, out var method);
            return method;
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["methods"] = new JArray(Methods.Select(u => u.ToJson()).ToArray());
            json["events"] = new JArray(Events.Select(u => u.ToJson()).ToArray());
            return json;
        }

        private void DeserializeFromJson(JObject json)
        {
            Methods = ((JArray)json["methods"]).Select(u => ContractMethodDescriptor.FromJson(u)).ToArray();
            Events = ((JArray)json["events"]).Select(u => ContractEventDescriptor.FromJson(u)).ToArray();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarString(ToString());
        }

        public void Deserialize(BinaryReader reader)
        {
            DeserializeFromJson(JObject.Parse(reader.ReadVarString(MaxLength)));
        }

        /// <summary>
        /// String representation
        /// </summary>
        /// <returns>Return json string</returns>
        public override string ToString() => ToJson().ToString();
    }
}
