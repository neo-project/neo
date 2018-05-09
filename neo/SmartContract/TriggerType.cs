namespace Neo.SmartContract
{
    public enum TriggerType : byte
    {
        Verification = 0x00,
        VerificationR = 0x01,
        Application = 0x10,
        ApplicationR = 0x11
    }
}
