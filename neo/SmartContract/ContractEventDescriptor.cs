using Neo.IO.Json;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    public class ContractEventDescriptor : IEquatable<ContractEventDescriptor>
    {
        /// <summary>
        /// Name is the name of the method, which can be any valid identifier.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parameters is an array of Parameter objects which describe the details of each parameter in the method.
        /// </summary>
        public ContractParameterDefinition[] Parameters { get; set; }

        public virtual bool Equals(ContractEventDescriptor other)
        {
            if (other == null) return false;
            if (ReferenceEquals(other, this)) return true;

            if (Name != other.Name) return false;
            if (!Parameters.SequenceEqual(other.Parameters)) return false;

            return true;
        }

        /// <summary>
        /// Parse ContractMethodDescription from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractMethodDescription</returns>
        public static ContractMethodDescriptor Parse(JObject json)
        {
            return new ContractMethodDescriptor
            {
                Name = json["name"].AsString(),
                Parameters = ((JArray)json["parameters"]).Select(u => ContractParameterDefinition.Parse(u)).ToArray(),
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