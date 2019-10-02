using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Actor;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;

namespace Neo.Network.RPC.Server
{
    [RpcController(Name = "$root")]
    public class NeoDefaultRpcController
    {
        [RpcMethod]
        public string getbestblockhash() {
            return Blockchain.Singleton.CurrentBlockHash.ToString();
        }

        [RpcMethod]
        public JObject getblock(string key, bool verbose = false) {
            Block block;
            if (int.TryParse(key, out _))
            {
                uint index = uint.Parse(key);
                block = Blockchain.Singleton.Store.GetBlock(index);
            }
            else
            {
                UInt256 hash = UInt256.Parse(key);
                block = Blockchain.Singleton.Store.GetBlock(hash);
            }

            if (block == null) throw new RpcException(-100, "Unknown block");

            if (verbose)
            {
                JObject json = block.ToJson();
                json["confirmations"] = Blockchain.Singleton.Height - block.Index + 1;
                UInt256 hash = Blockchain.Singleton.Store.GetNextBlockHash(block.Hash);
                if (hash != null)
                    json["nextblockhash"] = hash.ToString();
                return json;
            }
            return block.ToArray().ToHexString();
        }

        [RpcMethod]
        public uint getblockcount()
        {
            return Blockchain.Singleton.Height + 1;
        }

        [RpcMethod]
        public string getblockhash(uint height)
        {
            if (height <= Blockchain.Singleton.Height)
            {
                return Blockchain.Singleton.GetBlockHash(height).ToString();
            }
            throw new RpcException(-100, "Invalid Height");
        }

        [RpcMethod]
        public JObject getblockheader(string key, bool verbose = false)
        {
            Header header;
            if (int.TryParse(key, out _))
            {
                uint height = uint.Parse(key);
                header = Blockchain.Singleton.Store.GetHeader(height);
            }
            else
            {
                UInt256 hash = UInt256.Parse(key);
                header = Blockchain.Singleton.Store.GetHeader(hash);
            }

            if (header == null)
            {
                throw new RpcException(-100, "Unknown block");
            }

            if (verbose)
            {
                JObject json = header.ToJson();
                json["confirmations"] = Blockchain.Singleton.Height - header.Index + 1;
                UInt256 hash = Blockchain.Singleton.Store.GetNextBlockHash(header.Hash);
                if (hash != null)
                    json["nextblockhash"] = hash.ToString();
                return json;
            }

            return header.ToArray().ToHexString();
        }

        [RpcMethod]
        public string getblocksysfee(uint height)
        {
            if (height <= Blockchain.Singleton.Height)
                using (ApplicationEngine engine = NativeContract.GAS.TestCall("getSysFeeAmount", height))
                {
                    return engine.ResultStack.Peek().GetBigInteger().ToString();
                }
            throw new RpcException(-100, "Invalid Height");
        }

        [RpcMethod]
        public int getconnectioncount()
        {
            return LocalNode.Singleton.ConnectedCount;
        }

        [RpcMethod]
        public JObject getcontractstate(string script_hash)
        {
            ContractState contract = Blockchain.Singleton.Store.GetContracts().TryGet(UInt160.Parse(script_hash));
            return contract?.ToJson() ?? throw new RpcException(-100, "Unknown contract");
        }

        [RpcMethod]
        public JObject getpeers()
        {
            JObject json = new JObject();
            json["unconnected"] = new JArray(LocalNode.Singleton.GetUnconnectedPeers().Select(p =>
            {
                JObject peerJson = new JObject();
                peerJson["address"] = p.Address.ToString();
                peerJson["port"] = p.Port;
                return peerJson;
            }));
            json["bad"] = new JArray(); //badpeers has been removed
            json["connected"] = new JArray(LocalNode.Singleton.GetRemoteNodes().Select(p =>
            {
                JObject peerJson = new JObject();
                peerJson["address"] = p.Remote.Address.ToString();
                peerJson["port"] = p.ListenerTcpPort;
                return peerJson;
            }));
            return json;
        }

        [RpcMethod]
        public JObject getrawmempool(bool shouldGetUnverified)
        {
            if (!shouldGetUnverified)
                return new JArray(Blockchain.Singleton.MemPool.GetVerifiedTransactions().Select(p => (JObject)p.Hash.ToString()));

            JObject json = new JObject();
            json["height"] = Blockchain.Singleton.Height;
            Blockchain.Singleton.MemPool.GetVerifiedAndUnverifiedTransactions(
                out IEnumerable<Transaction> verifiedTransactions,
                out IEnumerable<Transaction> unverifiedTransactions);
            json["verified"] = new JArray(verifiedTransactions.Select(p => (JObject)p.Hash.ToString()));
            json["unverified"] = new JArray(unverifiedTransactions.Select(p => (JObject)p.Hash.ToString()));
            return json;
        }

        [RpcMethod]
        public JObject getrawtransaction(string hash, bool verbose = false)
        {
            UInt256 hash256 = UInt256.Parse(hash);
            Transaction tx = Blockchain.Singleton.GetTransaction(hash256);
            if (tx == null)
                throw new RpcException(-100, "Unknown transaction");
            if (verbose)
            {
                JObject json = tx.ToJson();
                TransactionState txState = Blockchain.Singleton.Store.GetTransactions().TryGet(hash256);
                if (txState != null)
                {
                    Header header = Blockchain.Singleton.Store.GetHeader(txState.BlockIndex);
                    json["blockhash"] = header.Hash.ToString();
                    json["confirmations"] = Blockchain.Singleton.Height - header.Index + 1;
                    json["blocktime"] = header.Timestamp;
                    json["vmState"] = txState.VMState;
                }
                return json;
            }
            return tx.ToArray().ToHexString();
        }

        [RpcMethod]
        public string getstorage(string script_hash, string key)
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = UInt160.Parse(script_hash),
                Key = key.HexToBytes()
            }) ?? new StorageItem();
            return item.Value?.ToHexString();
        }

        [RpcMethod]
        public uint gettransactionheight(string hash)
        {
            uint? height = Blockchain.Singleton.Store.GetTransactions().TryGet(UInt256.Parse(hash))?.BlockIndex;
            if (height.HasValue) return height.Value;
            throw new RpcException(-100, "Unknown transaction");
        }

        [RpcMethod]
        public JObject getvalidators()
        {
            using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
            {
                var validators = NativeContract.NEO.GetValidators(snapshot);
                return NativeContract.NEO.GetRegisteredValidators(snapshot).Select(p =>
                {
                    JObject validator = new JObject();
                    validator["publickey"] = p.PublicKey.ToString();
                    validator["votes"] = p.Votes.ToString();
                    validator["active"] = validators.Contains(p.PublicKey);
                    return validator;
                }).ToArray();
            }
        }

        [RpcMethod]
        public JObject getversion()
        {
            JObject json = new JObject();
            json["tcpPort"] = LocalNode.Singleton.ListenerTcpPort;
            json["wsPort"] = LocalNode.Singleton.ListenerWsPort;
            json["nonce"] = LocalNode.Nonce;
            json["useragent"] = LocalNode.UserAgent;
            return json;
        }

        [RpcMethod]
        public JObject invokefunction(NeoSystem system, string script_hash, string operation, object[] args)
        {
            ContractParameter[] cParams = args
                .Select(a => ContractParameter
                    .FromJson(JObject
                        .FromPrimitive(a)))
                .ToArray();

            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                script = sb.EmitAppCall(UInt160.Parse(script_hash), operation, cParams).ToArray();
            }

            return GetInvokeResult(system, script);
        }

        [RpcMethod]
        public JObject invokescript(NeoSystem system, string scriptHex, string[] hashesToVerify = null)
        {
            byte[] script = scriptHex.HexToBytes();

            CheckWitnessHashes checkWitnessHashes = null;
            if (hashesToVerify != null)
            {
                UInt160[] scriptHashesForVerifying = hashesToVerify.Select(UInt160.Parse).ToArray();
                checkWitnessHashes = new CheckWitnessHashes(scriptHashesForVerifying);
            }

            return GetInvokeResult(system, script, checkWitnessHashes);
        }

        [RpcMethod]
        public JObject listplugins()
        {
            return new JArray(Plugin.Plugins
                .OrderBy(u => u.Name)
                .Select(u => new JObject
                {
                    ["name"] = u.Name,
                    ["version"] = u.Version.ToString(),
                    ["interfaces"] = new JArray(u.GetType().GetInterfaces()
                        .Select(p => p.Name)
                        .Where(p => p.EndsWith("Plugin"))
                        .Select(p => (JObject)p))
                }));
        }

        [RpcMethod]
        public JObject sendrawtransaction(string txHash, NeoSystem system)
        {
            Transaction tx = txHash.HexToBytes().AsSerializable<Transaction>();
            RelayResultReason reason = system.Blockchain.Ask<RelayResultReason>(tx).Result;
            return GetRelayResult(reason, tx.Hash);
        }

        [RpcMethod]
        public JObject submitblock(string blockHash, NeoSystem system)
        {
            Block block = blockHash.HexToBytes().AsSerializable<Block>();
            RelayResultReason reason = system.Blockchain.Ask<RelayResultReason>(block).Result;
            return GetRelayResult(reason, block.Hash);
        }

        [RpcMethod]
        public JObject validateaddress(string address)
        {
            JObject json = new JObject();
            UInt160 scriptHash;
            try
            {
                scriptHash = address.ToScriptHash();
            }
            catch
            {
                scriptHash = null;
            }
            json["address"] = address;
            json["isvalid"] = scriptHash != null;
            return json;
        }

        /**
         * PRIVATE MEMBERS:
         */

        private static JObject GetRelayResult(RelayResultReason reason, UInt256 hash)
        {
            switch (reason)
            {
                case RelayResultReason.Succeed:
                {
                    var ret = new JObject();
                    ret["hash"] = hash.ToString();
                    return ret;
                }
                case RelayResultReason.AlreadyExists:
                    throw new RpcException(-501, "Block or transaction already exists and cannot be sent repeatedly.");
                case RelayResultReason.OutOfMemory:
                    throw new RpcException(-502, "The memory pool is full and no more transactions can be sent.");
                case RelayResultReason.UnableToVerify:
                    throw new RpcException(-503, "The block cannot be validated.");
                case RelayResultReason.Invalid:
                    throw new RpcException(-504, "Block or transaction validation failed.");
                case RelayResultReason.PolicyFail:
                    throw new RpcException(-505, "One of the Policy filters failed.");
                default:
                    throw new RpcException(-500, "Unknown error.");
            }
        }

        private static JObject GetInvokeResult(NeoSystem system, byte[] script, IVerifiable checkWitnessHashes = null)
        {
            ApplicationEngine engine = ApplicationEngine.Run(script, checkWitnessHashes, extraGAS: system.MaxGasInvoke);
            JObject json = new JObject();
            json["script"] = script.ToHexString();
            json["state"] = engine.State;
            json["gas_consumed"] = engine.GasConsumed.ToString();
            try
            {
                json["stack"] = new JArray(engine.ResultStack.Select(p => p.ToParameter().ToJson()));
            }
            catch (InvalidOperationException)
            {
                json["stack"] = "error: recursive reference";
            }
            return json;
        }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Plugin.Log(nameof(NeoDefaultRpcController), level, message);
        }

        private class CheckWitnessHashes : IVerifiable
        {
            private readonly UInt160[] _scriptHashesForVerifying;
            public Witness[] Witnesses { get; set; }
            public int Size { get; }

            public CheckWitnessHashes(UInt160[] scriptHashesForVerifying)
            {
                _scriptHashesForVerifying = scriptHashesForVerifying;
            }

            public void Serialize(BinaryWriter writer)
            {
                throw new NotImplementedException();
            }

            public void Deserialize(BinaryReader reader)
            {
                throw new NotImplementedException();
            }

            public void DeserializeUnsigned(BinaryReader reader)
            {
                throw new NotImplementedException();
            }

            public UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
            {
                return _scriptHashesForVerifying;
            }

            public void SerializeUnsigned(BinaryWriter writer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
