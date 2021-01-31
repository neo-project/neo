using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract.Manifest
{
    public class ContractParameterDefinition : IInteroperable
    {
        /// <summary>
        /// Name is the name of the parameter, which can be any valid identifier.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type indicates the type of the parameter. It can be one of the following values: 
        ///     Any, Signature, Boolean, Integer, Hash160, Hash256, ByteArray, PublicKey, String, Array, Map, InteropInterface.
        /// </summary>
        public ContractParameterType Type { get; set; }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Name = @struct[0].GetString();
            Type = (ContractParameterType)(byte)@struct[1].GetInteger();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { Name, (byte)Type };
        }

        /// <summary>
        /// Parse ContractParameterDefinition from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractParameterDefinition</returns>
        public static ContractParameterDefinition FromJson(JObject json)
        {
            ContractParameterDefinition parameter = new ContractParameterDefinition
            {
                Name = json["name"].AsString(),
                Type = (ContractParameterType)Enum.Parse(typeof(ContractParameterType), json["type"].AsString()),
            };
            if (string.IsNullOrEmpty(parameter.Name))
                throw new FormatException();
            if (!Enum.IsDefined(parameter.Type) || parameter.Type == ContractParameterType.Void)
                throw new FormatException();
            return parameter;
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
