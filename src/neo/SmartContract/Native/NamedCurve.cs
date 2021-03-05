namespace Neo.SmartContract.Native
{
    /// <summary>
    /// RFC 4492
    /// </summary>
    /// <remarks>
    /// https://tools.ietf.org/html/rfc4492#section-5.1.1
    /// </remarks>
    public enum NamedCurve : byte
    {
        secp256k1 = 22,
        secp256r1 = 23
    }
}
