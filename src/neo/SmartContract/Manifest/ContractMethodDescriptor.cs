using Neo.IO.Json;
using System;
using System.Linq;

namespace Neo.SmartContract.Manifest
{
    public class ContractMethodDescriptor : ContractEventDescriptor
    {
        private int _offset;

        public int Offset
        {
            get => _offset;
            set => _offset = value >= 0 ? value : throw new FormatException();
        }

        /// <summary>
        /// Returntype indicates the return type of the method. It can be one of the following values: 
        ///     Any, Signature, Boolean, Integer, Hash160, Hash256, ByteArray, PublicKey, String, Array, Map, InteropInterface, Void.
        /// </summary>
        public ContractParameterType ReturnType { get; set; }

        /// <summary>
        /// Determine if it's safe to call this method
        /// If a method is marked as safe, the user interface will not give any warnings when it is called by any other contract.
        /// </summary>
        public bool Safe { get; set; }

        public new ContractMethodDescriptor Clone()
        {
            return new ContractMethodDescriptor
            {
                Name = Name,
                Parameters = Parameters.Select(p => p.Clone()).ToArray(),
                Offset = Offset,
                ReturnType = ReturnType,
                Safe = Safe
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
                Offset = (int)json["offset"].AsNumber(),
                ReturnType = (ContractParameterType)Enum.Parse(typeof(ContractParameterType), json["returntype"].AsString()),
                Safe = json["safe"].AsBoolean(),
            };
        }

        public override JObject ToJson()
        {
            var json = base.ToJson();
            json["offset"] = Offset;
            json["returntype"] = ReturnType.ToString();
            json["safe"] = Safe;
            return json;
        }
    }
}
