using Neo.IO.Json;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// For technical details of ABI, please refer to NEP-3: NeoContract ABI. (https://github.com/neo-project/proposals/blob/master/nep-3.mediawiki)
    /// </summary>
    public class ContractAbi
    {
        private IReadOnlyDictionary<string, ContractMethodDescriptor> methodDictionary;

        /// <summary>
        /// Hash is the script hash of the contract. It is encoded as a hexadecimal string in big-endian.
        /// </summary>
        public UInt160 Hash { get; set; }

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
                Hash = Hash,
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
            return new ContractAbi
            {
                Hash = UInt160.Parse(json["hash"].AsString()),
                Methods = ((JArray)json["methods"]).Select(u => ContractMethodDescriptor.FromJson(u)).ToArray(),
                Events = ((JArray)json["events"]).Select(u => ContractEventDescriptor.FromJson(u)).ToArray()
            };
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
            json["hash"] = Hash.ToString();
            json["methods"] = new JArray(Methods.Select(u => u.ToJson()).ToArray());
            json["events"] = new JArray(Events.Select(u => u.ToJson()).ToArray());
            return json;
        }
    }
}
