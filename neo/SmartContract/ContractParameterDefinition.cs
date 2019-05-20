namespace Neo.SmartContract
{
    public class ContractParameterDefinition
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
    }
}