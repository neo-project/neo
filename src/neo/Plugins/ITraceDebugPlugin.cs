using Neo.Network.P2P.Payloads;

namespace Neo.Plugins
{
    public interface ITraceDebugPlugin
    {
        bool ShouldTrace(Header header, Transaction tx);
        ITraceDebugSink GetSink(Header header, Transaction tx);
    }
}
