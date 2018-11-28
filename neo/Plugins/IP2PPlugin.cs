namespace Neo.Plugins
{
    public interface IP2PPlugin
    {
        bool IsAllowed(string cmd, object payload);
    }
}