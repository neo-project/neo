using Neo.IO.Json;
using System.Linq;

namespace Neo.SmartContract.Manifest
{
    public class ContractEventDescriptor
    {
        /// <summary>
        /// Name is the name of the method, which can be any valid identifier.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parameters is an array of Parameter objects which describe the details of each parameter in the method.
        /// </summary>
        public ContractParameterDefinition[] Parameters { get; set; }

        public ContractEventDescriptor Clone()
        {
            return new ContractEventDescriptor
            {
                Name = Name,
                Parameters = Parameters.Select(p => p.Clone()).ToArray()
            };
        }

        /// <summary>
        /// Parse ContractEventDescriptor from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractEventDescriptor</returns>
        public static ContractEventDescriptor FromJson(JObject json)
        {
            return new ContractEventDescriptor
            {
                Name = json["name"].AsString(),
                Parameters = ((JArray)json["parameters"]).Select(u => ContractParameterDefinition.FromJson(u)).ToArray(),
            };
        }

        public virtual JObject ToJson()
        {
            var json = new JObject();
            json["name"] = Name;
            json["parameters"] = new JArray(Parameters.Select(u => u.ToJson()).ToArray());
            return json;
        }
    }
}
