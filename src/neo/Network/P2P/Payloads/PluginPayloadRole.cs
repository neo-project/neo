namespace Neo.Network.P2P.Payloads
{
    public enum PluginPayloadRole : byte
    {
        Committee = 0,
        Validators = 2,
        StateValidator = 4,
        Oracle = 8
    }
}
