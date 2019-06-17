using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SDK.RPC;
using Neo.SDK.RPC.Model;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using Neo.Persistence;

namespace Neo.SDK.TX
{
    /// <summary>
    /// This class helps to create transactions manually.
    /// </summary>
    public class TransactionHelper
    {
        private readonly IRpcClient _neoRpc;

        public TransactionHelper(IRpcClient neoRpc)
        {
            _neoRpc = neoRpc;
        }

        /// <summary>
        /// Call API method to get the balance of a specific NEP-5 token from a specific address.
        /// </summary>
        protected BigInteger GetNep5Balance(UInt160 assetId, UInt160 from)
        {
            string fromStr = from.ToAddress();
            string balance = _neoRpc.GetNep5Balances(fromStr).Balances.Where(n => UInt160.Parse(n.AssetHash) == assetId).First().Amount;
            return BigInteger.Parse(balance);
        }

    }
}