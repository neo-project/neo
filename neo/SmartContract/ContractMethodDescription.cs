using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Neo.SmartContract
{
    public class ContractMethodDescription : ContractActionDescription, IEquatable<ContractMethodDescription>
    {
        /// <summary>
        /// Returntype indicates the return type of the method. It can be one of the following values: 
        ///     Signature, Boolean, Integer, Hash160, Hash256, ByteArray, PublicKey, String, Array, InteropInterface, Void.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ContractParameterType ReturnType { get; set; }

        public override bool Equals(ContractActionDescription other)
        {
            if (!base.Equals(other)) return false;
            if (!(other is ContractMethodDescription b)) return false;
            if (ReturnType != b.ReturnType) return false;

            return true;
        }

        bool IEquatable<ContractMethodDescription>.Equals(ContractMethodDescription other)
        {
            return Equals((ContractActionDescription)other);
        }
    }
}