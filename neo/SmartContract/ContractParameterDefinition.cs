using Neo.IO.Json;
using System;

namespace Neo.SmartContract
{
    public class ContractParameterDefinition : IEquatable<ContractParameterDefinition>
    {
        /// <summary>
        /// Name is the name of the parameter, which can be any valid identifier.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type indicates the type of the parameter. It can be one of the following values: 
        ///     Signature, Boolean, Integer, Hash160, Hash256, ByteArray, PublicKey, String, Array, InteropInterface.
        /// </summary>
        public ContractParameterType Type { get; set; }

        public bool Equals(ContractParameterDefinition other)
        {
            if (other == null) return false;
            if (ReferenceEquals(other, this)) return true;

            if (Type != other.Type) return false;
            if (Name != other.Name) return false;

            return true;
        }

        /// <summary>
        /// Parse ContractParameterDefinition from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractParameterDefinition</returns>
        public static ContractParameterDefinition Parse(JObject json)
        {
            return new ContractParameterDefinition
            {
                Name = json["name"].AsString(),
                Type = (ContractParameterType)Enum.Parse(typeof(ContractParameterType), json["type"].AsString()),
            };
        }

        public virtual JObject ToJson()
        {
            var json = new JObject();
            json["name"] = Name;
            json["type"] = Type.ToString();
            return json;
        }
    }
}