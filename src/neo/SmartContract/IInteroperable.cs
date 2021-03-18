using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the object that can be converted to and from <see cref="StackItem"/>.
    /// </summary>
    public interface IInteroperable
    {
        /// <summary>
        /// Convert a <see cref="StackItem"/> to the current object.
        /// </summary>
        /// <param name="stackItem">The <see cref="StackItem"/> to convert.</param>
        void FromStackItem(StackItem stackItem);

        /// <summary>
        /// Convert the current object to a <see cref="StackItem"/>.
        /// </summary>
        /// <param name="referenceCounter">The <see cref="ReferenceCounter"/> used by the <see cref="StackItem"/>.</param>
        /// <returns>The converted <see cref="StackItem"/>.</returns>
        StackItem ToStackItem(ReferenceCounter referenceCounter);
    }
}
