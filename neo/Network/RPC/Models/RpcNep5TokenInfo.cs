using System.Numerics;

namespace Neo.Network.RPC.Models
{
    public class RpcNep5TokenInfo
    {
        public string Name { get; set; }

        public string Symbol { get; set; }

        public uint Decimals { get; set; }

        public BigInteger TotalSupply { get; set; }
    }
}
