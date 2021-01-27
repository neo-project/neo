using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;

namespace Neo.SmartContract.Manifest
{
    public class ContractMethodDescriptor : ContractEventDescriptor
    {
        /// <summary>
        /// Returntype indicates the return type of the method. It can be one of the following values: 
        ///     Any, Signature, Boolean, Integer, Hash160, Hash256, ByteArray, PublicKey, String, Array, Map, InteropInterface, Void.
        /// </summary>
        public ContractParameterType ReturnType { get; set; }

        public int Offset { get; set; }

        /// <summary>
        /// Determine if it's safe to call this method
        /// If a method is marked as safe, the user interface will not give any warnings when it is called by any other contract.
        /// </summary>
        public bool Safe { get; set; }

        public override void FromStackItem(StackItem stackItem)
        {
            base.FromStackItem(stackItem);
            Struct @struct = (Struct)stackItem;
            ReturnType = (ContractParameterType)(byte)@struct[2].GetInteger();
            Offset = (int)@struct[3].GetInteger();
            Safe = @struct[4].GetBoolean();
        }

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Struct @struct = (Struct)base.ToStackItem(referenceCounter);
            @struct.Add((byte)ReturnType);
            @struct.Add(Offset);
            @struct.Add(Safe);
            return @struct;
        }

        /// <summary>
        /// Parse ContractMethodDescription from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractMethodDescription</returns>
        public new static ContractMethodDescriptor FromJson(JObject json)
        {
            ContractMethodDescriptor descriptor = new ContractMethodDescriptor
            {
                Name = json["name"].AsString(),
                Parameters = ((JArray)json["parameters"]).Select(u => ContractParameterDefinition.FromJson(u)).ToArray(),
                ReturnType = (ContractParameterType)Enum.Parse(typeof(ContractParameterType), json["returntype"].AsString()),
                Offset = (int)json["offset"].AsNumber(),
                Safe = json["safe"].AsBoolean(),
            };
            if (string.IsNullOrEmpty(descriptor.Name)) throw new FormatException();
            _ = descriptor.Parameters.ToDictionary(p => p.Name);
            if (!Enum.IsDefined(descriptor.ReturnType)) throw new FormatException();
            if (descriptor.Offset < 0) throw new FormatException();
            return descriptor;
        }

        public override JObject ToJson()
        {
            var json = base.ToJson();
            json["returntype"] = ReturnType.ToString();
            json["offset"] = Offset;
            json["safe"] = Safe;
            return json;
        }
    }
}
