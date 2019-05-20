namespace Neo.SmartContract
{
    public class ContractMethodDefinition
    {
        /// <summary>
        /// Name is the name of the method, which can be any valid identifier.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parameters is an array of Parameter objects which describe the details of each parameter in the method.
        /// </summary>
        public ContractParameterDefinition[] Parameters { get; set; }
    }
}