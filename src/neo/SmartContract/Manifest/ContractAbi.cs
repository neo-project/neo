using Neo.IO.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// NeoContract ABI
    /// </summary>
    public class ContractAbi
    {
        private IReadOnlyDictionary<(string, int), ContractMethodDescriptor> methodDictionary;

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
            return new ContractAbi
            {
                Methods = ((JArray)json["methods"]).Select(u => ContractMethodDescriptor.FromJson(u)).ToArray(),
                Events = ((JArray)json["events"]).Select(u => ContractEventDescriptor.FromJson(u)).ToArray()
            };
        }

        public ContractMethodDescriptor GetMethod(string name, int pcount)
        {
            if (pcount < -1 || pcount > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(pcount));
            if (pcount >= 0)
            {
                methodDictionary ??= Methods.ToDictionary(p => (p.Name, p.Parameters.Length));
                methodDictionary.TryGetValue((name, pcount), out var method);
                return method;
            }
            else
            {
                return Methods.FirstOrDefault(p => p.Name == name);
            }
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["methods"] = new JArray(Methods.Select(u => u.ToJson()).ToArray());
            json["events"] = new JArray(Events.Select(u => u.ToJson()).ToArray());
            return json;
        }
    }
}
