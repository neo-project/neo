using System.IO;
using Neo.IO;
using Neo.Models;
using Neo.Persistence;

namespace Neo.Network.P2P.Payloads
{
    public interface IVerifiable : IWitnessed
    {
        InventoryType InventoryType { get; }
        UInt160[] GetScriptHashesForVerifying(StoreView snapshot);
        bool Verify(StoreView snapshot);
    }
}
