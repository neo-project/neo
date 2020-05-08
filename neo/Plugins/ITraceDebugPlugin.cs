using Neo.Network.P2P.Payloads;

namespace Neo.Plugins
{
    public interface ITraceDebugPlugin
    {
        bool ShouldTrace(Header header, InvocationTransaction tx);
        
        ITraceDebugSink GetSink(Header header, InvocationTransaction tx);
    }
}
