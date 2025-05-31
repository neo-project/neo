# Neo RpcClient API Reference

This document provides a comprehensive list of all public RPC methods supported by the Neo RpcClient library. Methods are organized by category and presented in table format for better readability.

## Table of Contents

- [Neo RpcClient API Reference](#neo-rpcclient-api-reference)
  - [Table of Contents](#table-of-contents)
  - [Core RPC Methods](#core-rpc-methods)
  - [Blockchain Methods](#blockchain-methods)
  - [Node Methods](#node-methods)
  - [Smart Contract Methods](#smart-contract-methods)
  - [Wallet Methods](#wallet-methods)
  - [NEP-17 Token Methods](#nep-17-token-methods)
  - [State Methods](#state-methods)
  - [Policy Methods](#policy-methods)
  - [Transaction Methods](#transaction-methods)

## Core RPC Methods

| Method | Description | Parameters | Return Type | Example |
|--------|-------------|------------|-------------|---------|
| `Send` | Sends a raw RPC request and returns the response | <ul><li>`request`: The RPC request to send</li><li>`throwOnError`: Whether to throw an exception if the response contains an error</li></ul> | `RpcResponse` | ```csharp<br>var request = new RpcRequest {<br>    Id = 1,<br>    JsonRpc = "2.0",<br>    Method = "getblockcount",<br>    Params = Array.Empty<JToken>()<br>};<br>var response = client.Send(request);``` |
| `SendAsync` | Sends a raw RPC request asynchronously | <ul><li>`request`: The RPC request to send</li><li>`throwOnError`: Whether to throw an exception if the response contains an error</li></ul> | `Task<RpcResponse>` | ```csharp<br>var response = await client.SendAsync(request);``` |
| `RpcSend` | Sends an RPC request with the specified method and parameters | <ul><li>`method`: The RPC method to call</li><li>`paraArgs`: The parameters to pass to the RPC method</li></ul> | `JToken` | ```csharp<br>var result = client.RpcSend("getblockcount");``` |
| `RpcSendAsync` | Sends an RPC request with the specified method and parameters asynchronously | <ul><li>`method`: The RPC method to call</li><li>`paraArgs`: The parameters to pass to the RPC method</li></ul> | `Task<JToken>` | ```csharp<br>var result = await client.RpcSendAsync("getblockcount");``` |

## Blockchain Methods

| Method | Description | Parameters | Return Type | Example |
|--------|-------------|------------|-------------|---------|
| `GetBestBlockHashAsync` | Returns the hash of the tallest block in the main chain | None | `Task<string>` | ```csharp<br>string bestBlockHash = await client.GetBestBlockHashAsync();``` |
| `GetBlockAsync` | Returns information about a block based on its hash or index | <ul><li>`hashOrIndex`: The block hash or index</li></ul> | `Task<RpcBlock>` | ```csharp<br>var block = await client.GetBlockAsync("0x1234...");``` |
| `GetBlockHexAsync` | Returns serialized block information as a hexadecimal string | <ul><li>`hashOrIndex`: The block hash or index</li></ul> | `Task<string>` | ```csharp<br>var blockHex = await client.GetBlockHexAsync("10000");``` |
| `GetBlockCountAsync` | Gets the number of blocks in the main chain | None | `Task<uint>` | ```csharp<br>uint blockCount = await client.GetBlockCountAsync();``` |
| `GetBlockHashAsync` | Returns the hash value of the corresponding block | <ul><li>`index`: The block index</li></ul> | `Task<string>` | ```csharp<br>string blockHash = await client.GetBlockHashAsync(10000);``` |
| `GetBlockHeaderAsync` | Returns block header information | <ul><li>`hashOrIndex`: The block hash or index</li></ul> | `Task<RpcBlockHeader>` | ```csharp<br>var header = await client.GetBlockHeaderAsync("0x1234...");``` |
| `GetBlockHeaderHexAsync` | Returns serialized block header as a hexadecimal string | <ul><li>`hashOrIndex`: The block hash or index</li></ul> | `Task<string>` | ```csharp<br>var headerHex = await client.GetBlockHeaderHexAsync("10000");``` | 
| `GetBlockHeaderCountAsync` | Gets the number of block headers in the main chain | None | `Task<uint>` | ```csharp<br>uint headerCount = await client.GetBlockHeaderCountAsync();``` |
| `GetContractStateAsync` | Queries contract information | <ul><li>`hash`: The contract script hash</li></ul> or <ul><li>`id`: The contract ID</li></ul> | `Task<ContractState>` | ```csharp<br>var contractState = await client.GetContractStateAsync("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5");``` |
| `GetNativeContractsAsync` | Gets all native contracts | None | `Task<ContractState[]>` | ```csharp<br>var nativeContracts = await client.GetNativeContractsAsync();``` |
| `GetRawMempoolAsync` | Obtains the list of unconfirmed transactions in memory | None | `Task<string[]>` | ```csharp<br>var txHashes = await client.GetRawMempoolAsync();``` |
| `GetRawMempoolBothAsync` | Obtains both verified and unverified transactions in memory | None | `Task<RpcRawMemPool>` | ```csharp<br>var mempool = await client.GetRawMempoolBothAsync();``` |
| `GetRawTransactionAsync` | Returns transaction information | <ul><li>`txHash`: The transaction hash</li></ul> | `Task<RpcTransaction>` | ```csharp<br>var tx = await client.GetRawTransactionAsync("0x1234...");``` |
| `GetRawTransactionHexAsync` | Returns serialized transaction as a hexadecimal string | <ul><li>`txHash`: The transaction hash</li></ul> | `Task<string>` | ```csharp<br>var txHex = await client.GetRawTransactionHexAsync("0x1234...");``` |
| `GetStorageAsync` | Returns the stored value for a key in a contract's storage | <ul><li>`scriptHashOrId`: The contract script hash or ID</li><li>`key`: The storage key</li></ul> | `Task<string>` | ```csharp<br>var value = await client.GetStorageAsync("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5", "myKey");``` |
| `GetTransactionHeightAsync` | Returns the block index where the transaction is found | <ul><li>`txHash`: The transaction hash</li></ul> | `Task<uint>` | ```csharp<br>uint height = await client.GetTransactionHeightAsync("0x1234...");``` |
| `GetNextBlockValidatorsAsync` | Returns the next NEO consensus nodes information | None | `Task<RpcValidator[]>` | ```csharp<br>var validators = await client.GetNextBlockValidatorsAsync();``` |
| `GetCommitteeAsync` | Returns the current NEO committee members | None | `Task<string[]>` | ```csharp<br>var committee = await client.GetCommitteeAsync();``` |

## Node Methods

| Method | Description | Parameters | Return Type | Example |
|--------|-------------|------------|-------------|---------|
| `GetConnectionCountAsync` | Gets the current number of connections for the node | None | `Task<int>` | ```csharp<br>int connections = await client.GetConnectionCountAsync();``` |
| `GetPeersAsync` | Gets the list of nodes connected to the node | None | `Task<RpcPeers>` | ```csharp<br>var peers = await client.GetPeersAsync();``` |
| `GetVersionAsync` | Returns version information about the node | None | `Task<RpcVersion>` | ```csharp<br>var version = await client.GetVersionAsync();``` |
| `SendRawTransactionAsync` | Broadcasts a transaction over the NEO network | <ul><li>`rawTransaction`: The serialized transaction</li></ul> or <ul><li>`transaction`: The transaction object</li></ul> | `Task<UInt256>` | ```csharp<br>var txHash = await client.SendRawTransactionAsync(transaction);``` |
| `SubmitBlockAsync` | Broadcasts a serialized block over the NEO network | <ul><li>`block`: The serialized block</li></ul> | `Task<UInt256>` | ```csharp<br>var blockHash = await client.SubmitBlockAsync(blockData);``` |

## Smart Contract Methods

| Method | Description | Parameters | Return Type | Example |
|--------|-------------|------------|-------------|---------|
| `InvokeFunctionAsync` | Calls a smart contract method | <ul><li>`scriptHash`: The script hash of the contract</li><li>`operation`: The operation to invoke</li><li>`stacks`: The parameters to pass</li><li>`signer`: Optional signers for the invocation</li></ul> | `Task<RpcInvokeResult>` | ```csharp<br>var result = await client.InvokeFunctionAsync(<br>    "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5",<br>    "balanceOf",<br>    new[] { new RpcStack { Type = "Hash160", Value = "0x0c7f31e3ecf2d2fa4d84b7e9455593bc7e54e9a2" } }<br>);``` |
| `InvokeScriptAsync` | Executes a script through the VM | <ul><li>`script`: The script to execute</li><li>`signers`: Optional signers for the script</li></ul> | `Task<RpcInvokeResult>` | ```csharp<br>var result = await client.InvokeScriptAsync(scriptBytes);``` |
| `GetUnclaimedGasAsync` | Returns the unclaimed GAS amount | <ul><li>`address`: The address to check</li></ul> | `Task<RpcUnclaimedGas>` | ```csharp<br>var unclaimedGas = await client.GetUnclaimedGasAsync("NZNos2WqwVfNUXNj5VEqvvPzAqze3RXyP3");``` |
| `CalculateNetworkFeeAsync` | Calculates the network fee | <ul><li>`tx`: The transaction</li></ul> | `Task<long>` | ```csharp<br>long fee = await client.CalculateNetworkFeeAsync(transaction);``` |

## Wallet Methods

| Method | Description | Parameters | Return Type | Example |
|--------|-------------|------------|-------------|---------|
| `GetUnclaimedGasAsync` | Gets the unclaimed GAS for an account | <ul><li>`account`: The account address, script hash, or public key string</li></ul> or <ul><li>`account`: The account script hash</li></ul> | `Task<decimal>` | ```csharp<br>var walletAPI = new WalletAPI(client);<br>decimal unclaimedGas = await walletAPI.GetUnclaimedGasAsync("NZNos2WqwVfNUXNj5VEqvvPzAqze3RXyP3");``` |
| `GetNeoBalanceAsync` | Gets the NEO balance for an account | <ul><li>`account`: The account address, script hash, or public key string</li></ul> | `Task<uint>` | ```csharp<br>uint neoBalance = await walletAPI.GetNeoBalanceAsync("NZNos2WqwVfNUXNj5VEqvvPzAqze3RXyP3");``` |
| `GetGasBalanceAsync` | Gets the GAS balance for an account | <ul><li>`account`: The account address, script hash, or public key string</li></ul> | `Task<decimal>` | ```csharp<br>decimal gasBalance = await walletAPI.GetGasBalanceAsync("NZNos2WqwVfNUXNj5VEqvvPzAqze3RXyP3");``` |
| `GetTokenBalanceAsync` | Gets the token balance for an account | <ul><li>`tokenHash`: The token script hash</li><li>`account`: The account address, script hash, or public key string</li></ul> | `Task<BigInteger>` | ```csharp<br>BigInteger tokenBalance = await walletAPI.GetTokenBalanceAsync(<br>    "0xd2a4cff31913016155e38e474a2c06d08be276cf",<br>    "NZNos2WqwVfNUXNj5VEqvvPzAqze3RXyP3"<br>);``` |
| `ClaimGasAsync` | Claims GAS by transferring NEO from an account to itself | <ul><li>`key`: The WIF or private key</li><li>`addAssert`: Add assert to script</li></ul> or <ul><li>`keyPair`: The key pair</li><li>`addAssert`: Add assert to script</li></ul> | `Task<Transaction>` | ```csharp<br>var tx = await walletAPI.ClaimGasAsync("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p");``` |
| `TransferAsync` | Transfers NEP-17 tokens | <ul><li>`tokenHash`: The token script hash</li><li>`fromKey`: The sender's private key</li><li>`toAddress`: The recipient's address</li><li>`amount`: The amount to transfer</li><li>`data`: Optional data for onPayment</li><li>`addAssert`: Add assert to script</li></ul> | `Task<Transaction>` | ```csharp<br>var tx = await walletAPI.TransferAsync(<br>    "0xd2a4cff31913016155e38e474a2c06d08be276cf",<br>    "KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p",<br>    "NZNos2WqwVfNUXNj5VEqvvPzAqze3RXyP3",<br>    10.5m<br>);``` |

## NEP-17 Token Methods

| Method | Description | Parameters | Return Type | Example |
|--------|-------------|------------|-------------|---------|
| `BalanceOfAsync` | Gets the balance of a NEP-17 token | <ul><li>`scriptHash`: The contract script hash</li><li>`account`: The account script hash</li></ul> | `Task<BigInteger>` | ```csharp<br>var nep17API = new Nep17API(client);<br>BigInteger balance = await nep17API.BalanceOfAsync(<br>    UInt160.Parse("0xd2a4cff31913016155e38e474a2c06d08be276cf"),<br>    UInt160.Parse("0x0c7f31e3ecf2d2fa4d84b7e9455593bc7e54e9a2")<br>);``` |
| `SymbolAsync` | Gets the symbol of a NEP-17 token | <ul><li>`scriptHash`: The contract script hash</li></ul> | `Task<string>` | ```csharp<br>string symbol = await nep17API.SymbolAsync(UInt160.Parse("0xd2a4cff31913016155e38e474a2c06d08be276cf"));``` |
| `DecimalsAsync` | Gets the decimals of a NEP-17 token | <ul><li>`scriptHash`: The contract script hash</li></ul> | `Task<byte>` | ```csharp<br>byte decimals = await nep17API.DecimalsAsync(UInt160.Parse("0xd2a4cff31913016155e38e474a2c06d08be276cf"));``` |
| `TotalSupplyAsync` | Gets the total supply of a NEP-17 token | <ul><li>`scriptHash`: The contract script hash</li></ul> | `Task<BigInteger>` | ```csharp<br>BigInteger totalSupply = await nep17API.TotalSupplyAsync(UInt160.Parse("0xd2a4cff31913016155e38e474a2c06d08be276cf"));``` |
| `GetTokenInfoAsync` | Gets comprehensive token information | <ul><li>`scriptHash`: The contract script hash</li></ul> or <ul><li>`contractHash`: The contract hash as a string</li></ul> | `Task<RpcNep17TokenInfo>` | ```csharp<br>var tokenInfo = await nep17API.GetTokenInfoAsync("0xd2a4cff31913016155e38e474a2c06d08be276cf");``` |
| `CreateTransferTxAsync` | Creates a NEP-17 token transfer transaction | <ul><li>`scriptHash`: The contract script hash</li><li>`fromKey`: The sender's key pair</li><li>`to`: The recipient's script hash</li><li>`amount`: The amount to transfer</li><li>`data`: Optional data for onPayment</li><li>`addAssert`: Add assert to script</li></ul> | `Task<Transaction>` | ```csharp<br>KeyPair keyPair = Utility.GetKeyPair("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p");<br>UInt160 to = Utility.GetScriptHash("NZNos2WqwVfNUXNj5VEqvvPzAqze3RXyP3", client.protocolSettings);<br>var tx = await nep17API.CreateTransferTxAsync(<br>    UInt160.Parse("0xd2a4cff31913016155e38e474a2c06d08be276cf"),<br>    keyPair,<br>    to,<br>    1000000000 // 10 GAS (with 8 decimals)<br>);``` |

## State Methods

| Method | Description | Parameters | Return Type | Example |
|--------|-------------|------------|-------------|---------|
| `GetStateRootAsync` | Gets the state root at a block index | <ul><li>`index`: The block index</li></ul> | `Task<RpcStateRoot>` | ```csharp<br>var stateAPI = new StateAPI(client);<br>var stateRoot = await stateAPI.GetStateRootAsync(10000);``` |
| `GetProofAsync` | Gets the proof for a key in a contract's storage | <ul><li>`rootHash`: The state root hash</li><li>`scriptHash`: The contract script hash</li><li>`key`: The storage key</li></ul> | `Task<byte[]>` | ```csharp<br>byte[] proof = await stateAPI.GetProofAsync(rootHash, scriptHash, keyBytes);``` |
| `VerifyProofAsync` | Verifies a storage proof | <ul><li>`rootHash`: The state root hash</li><li>`proofBytes`: The proof data</li></ul> | `Task<byte[]>` | ```csharp<br>byte[] value = await stateAPI.VerifyProofAsync(rootHash, proofBytes);``` |
| `GetStateHeightAsync` | Gets the current state height information | None | `Task<(uint? localRootIndex, uint? validatedRootIndex)>` | ```csharp<br>var (localRoot, validatedRoot) = await stateAPI.GetStateHeightAsync();``` |
| `FindStatesAsync` | Finds states in a contract's storage | <ul><li>`rootHash`: The state root hash</li><li>`scriptHash`: The contract script hash</li><li>`prefix`: The key prefix to search for</li><li>`from`: The key to start from (optional)</li><li>`count`: Max results (optional)</li></ul> | `Task<RpcFoundStates>` | ```csharp<br>var states = await stateAPI.FindStatesAsync(rootHash, scriptHash, prefixBytes);``` |
| `GetStateAsync` | Gets a value from a contract's storage | <ul><li>`rootHash`: The state root hash</li><li>`scriptHash`: The contract script hash</li><li>`key`: The storage key</li></ul> | `Task<byte[]>` | ```csharp<br>byte[] value = await stateAPI.GetStateAsync(rootHash, scriptHash, keyBytes);``` |

## Policy Methods

| Method | Description | Parameters | Return Type | Example |
|--------|-------------|------------|-------------|---------|
| `GetExecFeeFactor` | Gets the execution fee factor | None | `Task<long>` | ```csharp<br>var policyAPI = new PolicyAPI(client);<br>long execFeeFactor = await policyAPI.GetExecFeeFactor();``` |
| `GetStoragePrice` | Gets the storage price | None | `Task<long>` | ```csharp<br>long storagePrice = await policyAPI.GetStoragePrice();``` |
| `GetFeePerByte` | Gets the fee per byte | None | `Task<long>` | ```csharp<br>long feePerByte = await policyAPI.GetFeePerByte();``` |
| `IsBlocked` | Checks if a contract is blocked | <ul><li>`scriptHash`: The contract script hash</li></ul> | `Task<bool>` | ```csharp<br>bool isBlocked = await policyAPI.IsBlocked(scriptHash);``` |

## Transaction Methods

| Method | Description | Parameters | Return Type | Example |
|--------|-------------|------------|-------------|---------|
| `AddSignature` | Adds a signature to a transaction | <ul><li>`key`: The key pair</li></ul> | `TransactionManager` | ```csharp<br>var factory = new TransactionManagerFactory(client);<br>var manager = await factory.MakeTransactionAsync(script, signers);<br>manager.AddSignature(keyPair);``` |
| `AddMultiSig` | Adds multi-signature to a transaction | <ul><li>`keys`: The key pairs</li><li>`m`: The m value (m of n)</li><li>`pubKeys`: The public keys</li></ul> | `TransactionManager` | ```csharp<br>manager.AddMultiSig(keys, 2, pubKeys);``` |
| `AddWitness` | Adds a witness to a transaction | <ul><li>`scriptHash`: The script hash</li><li>`invocationScript`: The invocation script</li><li>`verificationScript`: The verification script</li></ul> | `TransactionManager` | ```csharp<br>manager.AddWitness(scriptHash, invocationScript, verificationScript);``` |
| `SignAsync` | Signs the transaction | None | `Task<Transaction>` | ```csharp<br>Transaction tx = await manager.SignAsync();``` 