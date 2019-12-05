using Neo.IO.Json;
using System;
using System.Linq;

namespace Neo.SmartContract.Manifest
{
    public class ContractMethodDescriptor : ContractEventDescriptor
    {
        /// <summary>
        /// Default entry point
        /// </summary>
        public static readonly ContractMethodDescriptor DefaultEntryPoint = new ContractMethodDescriptor()
        {
            Name = "Main",
            Parameters = new ContractParameterDefinition[]
            {
                new ContractParameterDefinition()
                {
                        Name = "operation",
                        Type = ContractParameterType.String
                },
                new ContractParameterDefinition()
                {
                        Name = "args",
                        Type = ContractParameterType.Array
                }
            },
            ReturnType = ContractParameterType.Any
        };

        /// <summary>
        /// Returntype indicates the return type of the method. It can be one of the following values: 
        ///     Signature, Boolean, Integer, Hash160, Hash256, ByteArray, PublicKey, String, Array, InteropInterface, Void.
        /// </summary>
        public ContractParameterType ReturnType { get; set; }

        public new ContractMethodDescriptor Clone()
        {
            return new ContractMethodDescriptor
            {
                Name = Name,
                Parameters = Parameters.Select(p => p.Clone()).ToArray(),
                ReturnType = ReturnType
            };
        }

        /// <summary>
        /// Parse ContractMethodDescription from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractMethodDescription</returns>
        public new static ContractMethodDescriptor FromJson(JObject json)
        {
            return new ContractMethodDescriptor
            {
                Name = json["name"].AsString(),
                Parameters = ((JArray)json["parameters"]).Select(u => ContractParameterDefinition.FromJson(u)).ToArray(),
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
