using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// The base class of the token states for <see cref="NonfungibleToken{TokenState}"/>.
    /// </summary>
    public abstract class NFTState : IInteroperable
    {
        /// <summary>
        /// The owner of the token.
        /// </summary>
        public UInt160 Owner;

        /// <summary>
        /// The name of the token.
        /// </summary>
        public string Name;

        /// <summary>
        /// The id of the token.
        /// </summary>
        public abstract byte[] Id { get; }

        /// <summary>
        /// Converts the token to a <see cref="Map"/>.
        /// </summary>
        /// <param name="referenceCounter">The <see cref="ReferenceCounter"/> used by the <see cref="Map"/>.</param>
        /// <returns>The converted map.</returns>
        public virtual Map ToMap(ReferenceCounter referenceCounter)
        {
            return new Map(referenceCounter) { ["name"] = Name };
        }

        public virtual void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Owner = new UInt160(@struct[0].GetSpan());
            Name = @struct[1].GetString();
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { Owner.ToArray(), Name };
        }
    }
}
