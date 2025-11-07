namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// interface is only used in conjuction with the InteropInterface type and MUST NOT be used for other types, when used it specifies which interop interface is used.
    /// The only valid defined value for it is "IIterator" which means an iterator object.
    /// When used it MUST be accompanied with the value object that specifies the type of each individual element returned from the iterator.
    /// </summary>
    public enum Nep25Interface
    {
        /// <summary>
        /// Iterator object
        /// </summary>
        IIterator
    }
}
