using Neo.IO.Json;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    public class ContractMethodDescriptor : ContractEventDescriptor, IEquatable<ContractMethodDescriptor>
    {
        /// <summary>
        /// Returntype indicates the return type of the method. It can be one of the following values: 
        ///     Signature, Boolean, Integer, Hash160, Hash256, ByteArray, PublicKey, String, Array, InteropInterface, Void.
        /// </summary>
        public ContractParameterType ReturnType { get; set; }

        public override bool Equals(ContractEventDescriptor other)
        {
            if (!base.Equals(other)) return false;
            if (!(other is ContractMethodDescriptor b)) return false;
            if (ReturnType != b.ReturnType) return false;

            return true;
        }

        bool IEquatable<ContractMethodDescriptor>.Equals(ContractMethodDescriptor other)
        {
            return Equals((ContractEventDescriptor)other);
        }

        /// <summary>
        /// Parse ContractMethodDescription from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractMethodDescription</returns>
        public new static ContractMethodDescriptor Parse(JObject json)
        {
            return new ContractMethodDescriptor
            {
                Name = json["name"].AsString(),
                Parameters = ((JArray)json["parameters"]).Select(u => ContractParameterDefinition.Parse(u)).ToArray(),
                ReturnType = (ContractParameterType)Enum.Parse(typeof(ContractParameterType), json["returnType"].AsString()),
            };
        }

        public override JObject ToJson()
        {
            var json = base.ToJson();
            json["returnType"] = ReturnType.ToString();
            return json;
        }
    }
}