namespace Neo.SmartContract
{
    public enum WitnessVerifyStrategy : byte
    {
        OnlyStateDependent = 0x1,
        OnlyStateIndependent = 0x2,
        All = 0x3
    }
}
