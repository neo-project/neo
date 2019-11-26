namespace Neo.Network.P2P.Payloads
{
    public enum DisconnectReason : byte
    {
        MaxConnectionReached = 0x01,
        MaxConnectionPerAddressReached = 0x02,
        DuplicateNonce = 0x03,

        MagicNumberIncompatible = 0x10,
        FormatException = 0x11,

        InternalError = 0x20,
    }
}
