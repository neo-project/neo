using Neo.SmartContract.Converters;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    /// <summary>
    /// For technical details of ABI, please refer to NEP-3: NeoContract ABI. (https://github.com/neo-project/proposals/blob/master/nep-3.mediawiki)
    /// </summary>
    public class ContractAbi : IEquatable<ContractAbi>
    {
        /// <summary>
        /// Hash is the script hash of the contract. It is encoded as a hexadecimal string in big-endian.
        /// </summary>
        [JsonConverter(typeof(Hash160JsonConverter))]
        public UInt160 Hash { get; set; }

        /// <summary>
        /// Entrypoint is a Method object which describe the details of the entrypoint of the contract.
        /// </summary>
        public ContractMethodDescription EntryPoint { get; set; }

        /// <summary>
        /// Methods is an array of Method objects which describe the details of each method in the contract.
        /// </summary>
        public ContractMethodDescription[] Methods { get; set; }

        /// <summary>
        /// Events is an array of Event objects which describe the details of each event in the contract.
        /// </summary>
        public ContractActionDescription[] Events { get; set; }

        public bool Equals(ContractAbi other)
        {
            if (other == null) return false;
            if (ReferenceEquals(other, this)) return true;

            if (!Hash.Equals(other.Hash)) return false;
            if (!EntryPoint.Equals(other.EntryPoint)) return false;
            if (!Methods.SequenceEqual(other.Methods)) return false;
            if (!Events.SequenceEqual(other.Events)) return false;

            return true;
        }
    }
}