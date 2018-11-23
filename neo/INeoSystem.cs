using System;
using Neo.Network.P2P.Payloads;
using Neo.Plugins;
using Neo.Wallets;
using System.Collections.Generic;

namespace Neo
{
    public interface INeoSystem : IDisposable
    {
        /*
        void TaskManagerRestartTasks(UInt256[] hashes);

        void LocalNodeRelay(IInventory block);

        void LocalNodeSendDirectly(IInventory payload);

        void LocalNodeRelayDirectly(IInventory payload);

        void LocalNodeSendInvMessage(InvPayload payload);
        */

        void Log(string source, LogLevel level, string message);

        void RelaySigned(ConsensusPayload payload, Wallet wallet);
    }
}
