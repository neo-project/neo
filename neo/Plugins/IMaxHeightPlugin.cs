using Neo.Network.P2P.Payloads;
using System.Collections.Generic;

namespace Neo.Plugins
{
    public interface IMaxHeightPlugin
    {
        bool void UpdateMaxHeight(out uint end);
    }
}
