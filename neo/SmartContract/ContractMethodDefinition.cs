using System;
using System.Linq;

namespace Neo.SmartContract
{
    public class ContractMethodDefinition : IEquatable<ContractMethodDefinition>
    {
        /// <summary>
        /// Name is the name of the method, which can be any valid identifier.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parameters is an array of Parameter objects which describe the details of each parameter in the method.
        /// </summary>
        public ContractParameterDefinition[] Parameters { get; set; }

        public virtual bool Equals(ContractMethodDefinition other)
        {
            if (other == null) return false;
            if (ReferenceEquals(other, this)) return true;

            if (Name != other.Name) return false;
            if (!Parameters.SequenceEqual(other.Parameters)) return false;

            return true;
        }
    }
}