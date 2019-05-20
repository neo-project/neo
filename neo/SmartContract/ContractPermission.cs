namespace Neo.SmartContract
{
    /// <summary>
    /// The permissions field is an array containing a set of Permission objects. It describes which contracts may be invoked and which methods are called.
    /// </summary>
    public class ContractPermission
    {
        /// <summary>
        /// The contract field indicates the contract to be invoked. It can be a hash of a contract, a public key of a group, or a wildcard *.
        /// If it specifies a hash of a contract, then the contract will be invoked; If it specifies a public key of a group, then any contract in this group will be invoked; If it specifies a wildcard*, then any contract will be invoked.
        /// </summary>
        public UInt160 Contract { get; set; }

        /// <summary>
        /// The methods field is an array containing a set of methods to be called. It can also be assigned with a wildcard *. If it is a wildcard *, then it means that any method can be called.
        /// If a contract invokes a contract or method that is not declared in the manifest at runtime, the invocation will fail.
        /// </summary>
        public string[] Methods { get; set; }
    }
}