namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents the named curve used in ECDSA.
    /// </summary>
    /// <remarks>
    /// https://tools.ietf.org/html/rfc4492#section-5.1.1
    /// </remarks>
    public enum NamedCurve : byte
    {
        /// <summary>
        /// The secp256k1 curve.
        /// </summary>
        secp256k1 = 22,

        /// <summary>
        /// The secp256r1 curve, which known as prime256v1 or nistP-256.
        /// </summary>
        secp256r1 = 23
    }
}
