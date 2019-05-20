namespace Neo.SmartContract
{
    public class ContractMethodWithReturnDefinition : ContractMethodDefinition
    {
        /// <summary>
        /// Returntype indicates the return type of the method. It can be one of the following values: 
        ///     Signature, Boolean, Integer, Hash160, Hash256, ByteArray, PublicKey, String, Array, InteropInterface, Void.
        /// </summary>
        public ContractParameterType ReturnType { get; set; }
    }
}