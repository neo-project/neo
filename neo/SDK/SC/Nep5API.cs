using Neo.Network.RPC;
using Neo.VM;
using System.Linq;
using System.Numerics;

namespace Neo.SDK.SC
{
    public class Nep5API : ContractClient
    {
        public Nep5API(RpcClient rpcClient) : base(rpcClient) { }

        public BigInteger BalanceOf(UInt160 scriptHash, UInt160 account)
        {
            BigInteger balance = TestInvoke(scriptHash, "balanceOf", account).Stack.Single().ToStackItem().GetBigInteger();
            return balance;
        }

        public string GetName(UInt160 scriptHash)
        {
            return TestInvoke(scriptHash, "name").Stack.Single().ToStackItem().GetString();
        }

        public string GetSymbol(UInt160 scriptHash)
        {
            return TestInvoke(scriptHash, "symbol").Stack.Single().ToStackItem().GetString();
        }

        public uint GetDecimals(UInt160 scriptHash)
        {
            return (uint)TestInvoke(scriptHash, "decimals").Stack.Single().ToStackItem().GetBigInteger();
        }

        public BigInteger GetTotalSupply(UInt160 scriptHash)
        {
            return TestInvoke(scriptHash, "totalSupply").Stack.Single().ToStackItem().GetBigInteger();
        }
    }
}
