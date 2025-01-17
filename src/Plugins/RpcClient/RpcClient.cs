// Copyright (C) 2015-2025 The Neo Project.
//
// RpcClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Neo.Network.RPC
{
    /// <summary>
    /// The RPC client to call NEO RPC methods
    /// </summary>
    public class RpcClient : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly Uri baseAddress;
        internal readonly ProtocolSettings protocolSettings;

        public RpcClient(Uri url, string rpcUser = default, string rpcPass = default, ProtocolSettings protocolSettings = null)
        {
            httpClient = new HttpClient();
            baseAddress = url;
            if (!string.IsNullOrEmpty(rpcUser) && !string.IsNullOrEmpty(rpcPass))
            {
                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{rpcUser}:{rpcPass}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
            }
            this.protocolSettings = protocolSettings ?? ProtocolSettings.Default;
        }

        public RpcClient(HttpClient client, Uri url, ProtocolSettings protocolSettings = null)
        {
            httpClient = client;
            baseAddress = url;
            this.protocolSettings = protocolSettings ?? ProtocolSettings.Default;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    httpClient.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        static RpcRequest AsRpcRequest(string method, params JToken[] paraArgs)
        {
            return new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = method,
                Params = paraArgs
            };
        }

        static RpcResponse AsRpcResponse(string content, bool throwOnError)
        {
            var response = RpcResponse.FromJson((JObject)JToken.Parse(content));
            response.RawResponse = content;

            if (response.Error != null && throwOnError)
            {
                throw new RpcException(response.Error.Code, response.Error.Message);
            }

            return response;
        }

        HttpRequestMessage AsHttpRequest(RpcRequest request)
        {
            var requestJson = request.ToJson().ToString();
            return new HttpRequestMessage(HttpMethod.Post, baseAddress)
            {
                Content = new StringContent(requestJson, Neo.Utility.StrictUTF8)
            };
        }

        public RpcResponse Send(RpcRequest request, bool throwOnError = true)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(RpcClient));

            using var requestMsg = AsHttpRequest(request);
            using var responseMsg = httpClient.Send(requestMsg);
            using var contentStream = responseMsg.Content.ReadAsStream();
            using var contentReader = new StreamReader(contentStream);
            return AsRpcResponse(contentReader.ReadToEnd(), throwOnError);
        }

        public async Task<RpcResponse> SendAsync(RpcRequest request, bool throwOnError = true)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(RpcClient));

            using var requestMsg = AsHttpRequest(request);
            using var responseMsg = await httpClient.SendAsync(requestMsg).ConfigureAwait(false);
            var content = await responseMsg.Content.ReadAsStringAsync();
            return AsRpcResponse(content, throwOnError);
        }

        public virtual JToken RpcSend(string method, params JToken[] paraArgs)
        {
            var request = AsRpcRequest(method, paraArgs);
            var response = Send(request);
            return response.Result;
        }

        public virtual async Task<JToken> RpcSendAsync(string method, params JToken[] paraArgs)
        {
            var request = AsRpcRequest(method, paraArgs);
            var response = await SendAsync(request).ConfigureAwait(false);
            return response.Result;
        }

        public static string GetRpcName([CallerMemberName] string methodName = null)
        {
            return new Regex("(.*?)(Hex|Both)?(Async)?").Replace(methodName, "$1").ToLowerInvariant();
        }

        #region Blockchain

        /// <summary>
        /// Returns the hash of the tallest block in the main chain.
        /// </summary>
        public async Task<string> GetBestBlockHashAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return result.AsString();
        }

        /// <summary>
        /// Returns the hash of the tallest block in the main chain.
        /// The serialized information of the block is returned, represented by a hexadecimal string.
        /// </summary>
        public async Task<string> GetBlockHexAsync(string hashOrIndex)
        {
            var result = int.TryParse(hashOrIndex, out int index)
                ? await RpcSendAsync(GetRpcName(), index).ConfigureAwait(false)
                : await RpcSendAsync(GetRpcName(), hashOrIndex).ConfigureAwait(false);
            return result.AsString();
        }

        /// <summary>
        /// Returns the hash of the tallest block in the main chain.
        /// </summary>
        public async Task<RpcBlock> GetBlockAsync(string hashOrIndex)
        {
            var result = int.TryParse(hashOrIndex, out int index)
                ? await RpcSendAsync(GetRpcName(), index, true).ConfigureAwait(false)
                : await RpcSendAsync(GetRpcName(), hashOrIndex, true).ConfigureAwait(false);

            return RpcBlock.FromJson((JObject)result, protocolSettings);
        }

        /// <summary>
        /// Gets the number of block header in the main chain.
        /// </summary>
        public async Task<uint> GetBlockHeaderCountAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return (uint)result.AsNumber();
        }

        /// <summary>
        /// Gets the number of blocks in the main chain.
        /// </summary>
        public async Task<uint> GetBlockCountAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return (uint)result.AsNumber();
        }

        /// <summary>
        /// Returns the hash value of the corresponding block, based on the specified index.
        /// </summary>
        public async Task<string> GetBlockHashAsync(uint index)
        {
            var result = await RpcSendAsync(GetRpcName(), index).ConfigureAwait(false);
            return result.AsString();
        }

        /// <summary>
        /// Returns the corresponding block header information according to the specified script hash.
        /// </summary>
        public async Task<string> GetBlockHeaderHexAsync(string hashOrIndex)
        {
            var result = int.TryParse(hashOrIndex, out int index)
                ? await RpcSendAsync(GetRpcName(), index).ConfigureAwait(false)
                : await RpcSendAsync(GetRpcName(), hashOrIndex).ConfigureAwait(false);
            return result.AsString();
        }

        /// <summary>
        /// Returns the corresponding block header information according to the specified script hash.
        /// </summary>
        public async Task<RpcBlockHeader> GetBlockHeaderAsync(string hashOrIndex)
        {
            var result = int.TryParse(hashOrIndex, out int index)
                ? await RpcSendAsync(GetRpcName(), index, true).ConfigureAwait(false)
                : await RpcSendAsync(GetRpcName(), hashOrIndex, true).ConfigureAwait(false);

            return RpcBlockHeader.FromJson((JObject)result, protocolSettings);
        }

        /// <summary>
        /// Queries contract information, according to the contract script hash.
        /// </summary>
        public async Task<ContractState> GetContractStateAsync(string hash)
        {
            var result = await RpcSendAsync(GetRpcName(), hash).ConfigureAwait(false);
            return ContractStateFromJson((JObject)result);
        }

        /// <summary>
        /// Queries contract information, according to the contract id.
        /// </summary>
        public async Task<ContractState> GetContractStateAsync(int id)
        {
            var result = await RpcSendAsync(GetRpcName(), id).ConfigureAwait(false);
            return ContractStateFromJson((JObject)result);
        }

        public static ContractState ContractStateFromJson(JObject json)
        {
            return new ContractState
            {
                Id = (int)json["id"].AsNumber(),
                UpdateCounter = (ushort)(json["updatecounter"]?.AsNumber() ?? 0),
                Hash = UInt160.Parse(json["hash"].AsString()),
                Nef = RpcNefFile.FromJson((JObject)json["nef"]),
                Manifest = ContractManifest.FromJson((JObject)json["manifest"])
            };
        }

        /// <summary>
        /// Get all native contracts.
        /// </summary>
        public async Task<ContractState[]> GetNativeContractsAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return ((JArray)result).Select(p => ContractStateFromJson((JObject)p)).ToArray();
        }

        /// <summary>
        /// Obtains the list of unconfirmed transactions in memory.
        /// </summary>
        public async Task<string[]> GetRawMempoolAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return ((JArray)result).Select(p => p.AsString()).ToArray();
        }

        /// <summary>
        /// Obtains the list of unconfirmed transactions in memory.
        /// shouldGetUnverified = true
        /// </summary>
        public async Task<RpcRawMemPool> GetRawMempoolBothAsync()
        {
            var result = await RpcSendAsync(GetRpcName(), true).ConfigureAwait(false);
            return RpcRawMemPool.FromJson((JObject)result);
        }

        /// <summary>
        /// Returns the corresponding transaction information, based on the specified hash value.
        /// </summary>
        public async Task<string> GetRawTransactionHexAsync(string txHash)
        {
            var result = await RpcSendAsync(GetRpcName(), txHash).ConfigureAwait(false);
            return result.AsString();
        }

        /// <summary>
        /// Returns the corresponding transaction information, based on the specified hash value.
        /// verbose = true
        /// </summary>
        public async Task<RpcTransaction> GetRawTransactionAsync(string txHash)
        {
            var result = await RpcSendAsync(GetRpcName(), txHash, true).ConfigureAwait(false);
            return RpcTransaction.FromJson((JObject)result, protocolSettings);
        }

        /// <summary>
        /// Calculate network fee
        /// </summary>
        /// <param name="tx">Transaction</param>
        /// <returns>NetworkFee</returns>
        public async Task<long> CalculateNetworkFeeAsync(Transaction tx)
        {
            var json = await RpcSendAsync(GetRpcName(), Convert.ToBase64String(tx.ToArray()))
                .ConfigureAwait(false);
            return (long)json["networkfee"].AsNumber();
        }

        /// <summary>
        /// Returns the stored value, according to the contract script hash (or Id) and the stored key.
        /// </summary>
        public async Task<string> GetStorageAsync(string scriptHashOrId, string key)
        {
            var result = int.TryParse(scriptHashOrId, out int id)
                ? await RpcSendAsync(GetRpcName(), id, key).ConfigureAwait(false)
                : await RpcSendAsync(GetRpcName(), scriptHashOrId, key).ConfigureAwait(false);
            return result.AsString();
        }

        /// <summary>
        /// Returns the block index in which the transaction is found.
        /// </summary>
        public async Task<uint> GetTransactionHeightAsync(string txHash)
        {
            var result = await RpcSendAsync(GetRpcName(), txHash).ConfigureAwait(false);
            return uint.Parse(result.AsString());
        }

        /// <summary>
        /// Returns the next NEO consensus nodes information and voting status.
        /// </summary>
        public async Task<RpcValidator[]> GetNextBlockValidatorsAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return ((JArray)result).Select(p => RpcValidator.FromJson((JObject)p)).ToArray();
        }

        /// <summary>
        /// Returns the current NEO committee members.
        /// </summary>
        public async Task<string[]> GetCommitteeAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return ((JArray)result).Select(p => p.AsString()).ToArray();
        }

        #endregion Blockchain

        #region Node

        /// <summary>
        /// Gets the current number of connections for the node.
        /// </summary>
        public async Task<int> GetConnectionCountAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return (int)result.AsNumber();
        }

        /// <summary>
        /// Gets the list of nodes that the node is currently connected/disconnected from.
        /// </summary>
        public async Task<RpcPeers> GetPeersAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return RpcPeers.FromJson((JObject)result);
        }

        /// <summary>
        /// Returns the version information about the queried node.
        /// </summary>
        public async Task<RpcVersion> GetVersionAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return RpcVersion.FromJson((JObject)result);
        }

        /// <summary>
        /// Broadcasts a serialized transaction over the NEO network.
        /// </summary>
        public async Task<UInt256> SendRawTransactionAsync(byte[] rawTransaction)
        {
            var result = await RpcSendAsync(GetRpcName(), Convert.ToBase64String(rawTransaction)).ConfigureAwait(false);
            return UInt256.Parse(result["hash"].AsString());
        }

        /// <summary>
        /// Broadcasts a transaction over the NEO network.
        /// </summary>
        public Task<UInt256> SendRawTransactionAsync(Transaction transaction)
        {
            return SendRawTransactionAsync(transaction.ToArray());
        }

        /// <summary>
        /// Broadcasts a serialized block over the NEO network.
        /// </summary>
        public async Task<UInt256> SubmitBlockAsync(byte[] block)
        {
            var result = await RpcSendAsync(GetRpcName(), Convert.ToBase64String(block)).ConfigureAwait(false);
            return UInt256.Parse(result["hash"].AsString());
        }

        #endregion Node

        #region SmartContract

        /// <summary>
        /// Returns the result after calling a smart contract at scripthash with the given operation and parameters.
        /// This RPC call does not affect the blockchain in any way.
        /// </summary>
        public async Task<RpcInvokeResult> InvokeFunctionAsync(string scriptHash, string operation, RpcStack[] stacks, params Signer[] signer)
        {
            List<JToken> parameters = new() { scriptHash.AsScriptHash(), operation, stacks.Select(p => p.ToJson()).ToArray() };
            if (signer.Length > 0)
            {
                parameters.Add(signer.Select(p => p.ToJson()).ToArray());
            }
            var result = await RpcSendAsync(GetRpcName(), parameters.ToArray()).ConfigureAwait(false);
            return RpcInvokeResult.FromJson((JObject)result);
        }

        /// <summary>
        /// Returns the result after passing a script through the VM.
        /// This RPC call does not affect the blockchain in any way.
        /// </summary>
        public async Task<RpcInvokeResult> InvokeScriptAsync(ReadOnlyMemory<byte> script, params Signer[] signers)
        {
            List<JToken> parameters = new() { Convert.ToBase64String(script.Span) };
            if (signers.Length > 0)
            {
                parameters.Add(signers.Select(p => p.ToJson()).ToArray());
            }
            var result = await RpcSendAsync(GetRpcName(), parameters.ToArray()).ConfigureAwait(false);
            return RpcInvokeResult.FromJson((JObject)result);
        }

        public async Task<RpcUnclaimedGas> GetUnclaimedGasAsync(string address)
        {
            var result = await RpcSendAsync(GetRpcName(), address.AsScriptHash()).ConfigureAwait(false);
            return RpcUnclaimedGas.FromJson((JObject)result);
        }


        public async IAsyncEnumerable<JObject> TraverseIteratorAsync(string sessionId, string id)
        {
            const int count = 100;
            while (true)
            {
                var result = await RpcSendAsync(GetRpcName(), sessionId, id, count).ConfigureAwait(false);
                var array = (JArray)result;
                foreach (JObject jObject in array)
                {
                    yield return jObject;
                }
                if (array.Count < count) break;
            }
        }

        /// <summary>
        /// Returns limit <paramref name="count"/> results from Iterator.
        /// This RPC call does not affect the blockchain in any way.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="id"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<JObject> TraverseIteratorAsync(string sessionId, string id, int count)
        {
            var result = await RpcSendAsync(GetRpcName(), sessionId, id, count).ConfigureAwait(false);
            if (result is JArray { Count: > 0 } array)
            {
                foreach (JObject jObject in array)
                {
                    yield return jObject;
                }
            }
        }

        /// <summary>
        /// Terminate specified Iterator session.
        /// This RPC call does not affect the blockchain in any way.
        /// </summary>
        public async Task<bool> TerminateSessionAsync(string sessionId)
        {
            var result = await RpcSendAsync(GetRpcName(), sessionId).ConfigureAwait(false);
            return result.GetBoolean();
        }

        #endregion SmartContract

        #region Utilities

        /// <summary>
        /// Returns a list of plugins loaded by the node.
        /// </summary>
        public async Task<RpcPlugin[]> ListPluginsAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return ((JArray)result).Select(p => RpcPlugin.FromJson((JObject)p)).ToArray();
        }

        /// <summary>
        /// Verifies that the address is a correct NEO address.
        /// </summary>
        public async Task<RpcValidateAddressResult> ValidateAddressAsync(string address)
        {
            var result = await RpcSendAsync(GetRpcName(), address).ConfigureAwait(false);
            return RpcValidateAddressResult.FromJson((JObject)result);
        }

        #endregion Utilities

        #region Wallet

        /// <summary>
        /// Close the wallet opened by RPC.
        /// </summary>
        public async Task<bool> CloseWalletAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return result.AsBoolean();
        }

        /// <summary>
        /// Exports the private key of the specified address.
        /// </summary>
        public async Task<string> DumpPrivKeyAsync(string address)
        {
            var result = await RpcSendAsync(GetRpcName(), address).ConfigureAwait(false);
            return result.AsString();
        }

        /// <summary>
        /// Creates a new account in the wallet opened by RPC.
        /// </summary>
        public async Task<string> GetNewAddressAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return result.AsString();
        }

        /// <summary>
        /// Returns the balance of the corresponding asset in the wallet, based on the specified asset Id.
        /// This method applies to assets that conform to NEP-17 standards.
        /// </summary>
        /// <returns>new address as string</returns>
        public async Task<BigDecimal> GetWalletBalanceAsync(string assetId)
        {
            var result = await RpcSendAsync(GetRpcName(), assetId).ConfigureAwait(false);
            BigInteger balance = BigInteger.Parse(result["balance"].AsString());
            byte decimals = await new Nep17API(this).DecimalsAsync(UInt160.Parse(assetId.AsScriptHash())).ConfigureAwait(false);
            return new BigDecimal(balance, decimals);
        }

        /// <summary>
        /// Gets the amount of unclaimed GAS in the wallet.
        /// </summary>
        public async Task<BigDecimal> GetWalletUnclaimedGasAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return BigDecimal.Parse(result.AsString(), SmartContract.Native.NativeContract.GAS.Decimals);
        }

        /// <summary>
        /// Imports the private key to the wallet.
        /// </summary>
        public async Task<RpcAccount> ImportPrivKeyAsync(string wif)
        {
            var result = await RpcSendAsync(GetRpcName(), wif).ConfigureAwait(false);
            return RpcAccount.FromJson((JObject)result);
        }

        /// <summary>
        /// Lists all the accounts in the current wallet.
        /// </summary>
        public async Task<List<RpcAccount>> ListAddressAsync()
        {
            var result = await RpcSendAsync(GetRpcName()).ConfigureAwait(false);
            return ((JArray)result).Select(p => RpcAccount.FromJson((JObject)p)).ToList();
        }

        /// <summary>
        /// Open wallet file in the provider's machine.
        /// By default, this method is disabled by RpcServer config.json.
        /// </summary>
        public async Task<bool> OpenWalletAsync(string path, string password)
        {
            var result = await RpcSendAsync(GetRpcName(), path, password).ConfigureAwait(false);
            return result.AsBoolean();
        }

        /// <summary>
        /// Transfer from the specified address to the destination address.
        /// </summary>
        /// <returns>This function returns Signed Transaction JSON if successful, ContractParametersContext JSON if signing failed.</returns>
        public async Task<JObject> SendFromAsync(string assetId, string fromAddress, string toAddress, string amount)
        {
            return (JObject)await RpcSendAsync(GetRpcName(), assetId.AsScriptHash(), fromAddress.AsScriptHash(),
                                      toAddress.AsScriptHash(), amount).ConfigureAwait(false);
        }

        /// <summary>
        /// Bulk transfer order, and you can specify a sender address.
        /// </summary>
        /// <returns>This function returns Signed Transaction JSON if successful, ContractParametersContext JSON if signing failed.</returns>
        public async Task<JObject> SendManyAsync(string fromAddress, IEnumerable<RpcTransferOut> outputs)
        {
            var parameters = new List<JToken>();
            if (!string.IsNullOrEmpty(fromAddress))
            {
                parameters.Add(fromAddress.AsScriptHash());
            }
            parameters.Add(outputs.Select(p => p.ToJson(protocolSettings)).ToArray());

            return (JObject)await RpcSendAsync(GetRpcName(), paraArgs: parameters.ToArray()).ConfigureAwait(false);
        }

        /// <summary>
        /// Transfer asset from the wallet to the destination address.
        /// </summary>
        /// <returns>This function returns Signed Transaction JSON if successful, ContractParametersContext JSON if signing failed.</returns>
        public async Task<JObject> SendToAddressAsync(string assetId, string address, string amount)
        {
            return (JObject)await RpcSendAsync(GetRpcName(), assetId.AsScriptHash(), address.AsScriptHash(), amount)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Cancel Tx.
        /// </summary>
        /// <returns>This function returns Signed Transaction JSON if successful, ContractParametersContext JSON if signing failed.</returns>
        public async Task<JObject> CancelTransactionAsync(UInt256 txId, string[] signers, string extraFee)
        {
            JToken[] parameters = signers.Select(s => (JString)s.AsScriptHash()).ToArray();
            return (JObject)await RpcSendAsync(GetRpcName(), txId.ToString(), new JArray(parameters), extraFee).ConfigureAwait(false);
        }

        #endregion Wallet

        #region Plugins

        /// <summary>
        /// Returns the contract log based on the specified txHash. The complete contract logs are stored under the ApplicationLogs directory.
        /// This method is provided by the plugin ApplicationLogs.
        /// </summary>
        public async Task<RpcApplicationLog> GetApplicationLogAsync(string txHash)
        {
            var result = await RpcSendAsync(GetRpcName(), txHash).ConfigureAwait(false);
            return RpcApplicationLog.FromJson((JObject)result, protocolSettings);
        }

        /// <summary>
        /// Returns the contract log based on the specified txHash. The complete contract logs are stored under the ApplicationLogs directory.
        /// This method is provided by the plugin ApplicationLogs.
        /// </summary>
        public async Task<RpcApplicationLog> GetApplicationLogAsync(string txHash, TriggerType triggerType)
        {
            var result = await RpcSendAsync(GetRpcName(), txHash, triggerType).ConfigureAwait(false);
            return RpcApplicationLog.FromJson((JObject)result, protocolSettings);
        }

        /// <summary>
        /// Returns all the NEP-17 transaction information occurred in the specified address.
        /// This method is provided by the plugin RpcNep17Tracker.
        /// </summary>
        /// <param name="address">The address to query the transaction information.</param>
        /// <param name="startTimestamp">The start block Timestamp, default to seven days before UtcNow</param>
        /// <param name="endTimestamp">The end block Timestamp, default to UtcNow</param>
        public async Task<RpcNep17Transfers> GetNep17TransfersAsync(string address, ulong? startTimestamp = default, ulong? endTimestamp = default)
        {
            startTimestamp ??= 0;
            endTimestamp ??= DateTime.UtcNow.ToTimestampMS();
            var result = await RpcSendAsync(GetRpcName(), address.AsScriptHash(), startTimestamp, endTimestamp)
                .ConfigureAwait(false);
            return RpcNep17Transfers.FromJson((JObject)result, protocolSettings);
        }

        /// <summary>
        /// Returns the balance of all NEP-17 assets in the specified address.
        /// This method is provided by the plugin RpcNep17Tracker.
        /// </summary>
        public async Task<RpcNep17Balances> GetNep17BalancesAsync(string address)
        {
            var result = await RpcSendAsync(GetRpcName(), address.AsScriptHash())
                .ConfigureAwait(false);
            return RpcNep17Balances.FromJson((JObject)result, protocolSettings);
        }

        #endregion Plugins
    }
}
