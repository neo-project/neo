using Neo.SmartContract.Converters;
using Newtonsoft.Json;
using System;
using System.Linq;

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
        [JsonConverter(typeof(Hash160JsonConverter))]
        public UInt160 Contract { get; set; }

        /// <summary>
        /// The methods field is an array containing a set of methods to be called. It can also be assigned with a wildcard *. If it is a wildcard *, then it means that any method can be called.
        /// If a contract invokes a contract or method that is not declared in the manifest at runtime, the invocation will fail.
        /// </summary>
        [JsonConverter(typeof(WillCardJsonConverter<string>))]
        public WildCardContainer<string> Methods { get; set; }

        /// <summary>
        /// Return true if is allowed
        /// </summary>
        /// <param name="contractHash">Contract hash</param>
        /// <param name="method">Method</param>
        /// <returns>Return true or false</returns>
        public bool IsAllowed(UInt160 contractHash, string method)
        {
            if (!Contract.Equals(contractHash))
            {
                // 0x00 = * wildcard

                if (Contract != UInt160.Zero) return false;
            }

            return Methods == null || Methods.IsWildcard || Methods.Contains(method);
        }
    }
}