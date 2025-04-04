# Neo RpcClient

## Overview

The Neo RpcClient is a .NET library for interacting with the Neo N3 blockchain through its RPC (Remote Procedure Call) interface. This component is part of the Neo blockchain toolkit and enables developers to integrate Neo blockchain functionality into their .NET applications by providing a type-safe, intuitive API for accessing Neo node services.

This library is organized within the Neo Plugins namespace but functions as a client SDK rather than a node plugin. It allows applications to communicate with Neo nodes running the RpcServer plugin without having to implement the node functionality themselves.

The RpcClient handles all aspects of RPC communication, transaction creation, signing, and submission, as well as specialized APIs for common operations like NEP-17 token transfers, wallet management, and smart contract interaction.

## Features

- Complete implementation of Neo N3 JSON-RPC API client methods
- Type-safe transaction creation and signing
- NEP-17 token operations (transfers, balance checking)
- Wallet management and operations
- Smart contract invocation and testing
- Transaction building and management
- Blockchain state querying

## Installation

Add the RpcClient to your project using NuGet:

```bash
dotnet add package Neo.Network.RPC.RpcClient
```

## API Reference

The library is organized into several specialized API classes, each focusing on a specific area of functionality:

### Core Components

- **RpcClient**: The main class for making RPC calls to Neo nodes
- **WalletAPI**: Utilities for wallet management and token operations
- **Nep17API**: NEP-17 token standard operations
- **TransactionManager**: Advanced transaction building and signing
- **ContractClient**: Base class for smart contract interaction
- **StateAPI**: For querying blockchain state
- **PolicyAPI**: For querying network policy parameters

### Key Classes and Methods

#### RpcClient

```csharp
// Initialize an RPC client
var client = new RpcClient(new Uri("http://seed1.neo.org:10332"));

// With authentication
var client = new RpcClient(new Uri("http://seed1.neo.org:10332"), "username", "password");
```

Primary methods:
- Blockchain queries (blocks, transactions, contract state)
- Transaction submission
- Smart contract invocation
- Network status information

#### WalletAPI

```csharp
var walletAPI = new WalletAPI(rpcClient);
```

Primary methods:
- `GetUnclaimedGasAsync`: Check unclaimed GAS
- `GetNeoBalanceAsync`: Check NEO balance
- `GetGasBalanceAsync`: Check GAS balance
- `ClaimGasAsync`: Claim GAS rewards
- `TransferAsync`: Transfer NEP-17 tokens

#### Nep17API

```csharp
var nep17API = new Nep17API(rpcClient);
```

Primary methods:
- `BalanceOfAsync`: Get token balance
- `SymbolAsync`: Get token symbol
- `DecimalsAsync`: Get token decimals
- `TotalSupplyAsync`: Get token total supply
- `GetTokenInfoAsync`: Get comprehensive token information
- `CreateTransferTxAsync`: Create token transfer transactions

#### TransactionManager

Handles the creation, signing, and submission of complex transactions.

## Usage Examples

### Basic Connection

```csharp
using Neo.Network.RPC;

// Connect to a Neo node
var client = new RpcClient(new Uri("http://localhost:10332"));

// Get current block height
uint blockCount = await client.GetBlockCountAsync();
Console.WriteLine($"Current block height: {blockCount - 1}");
```

### Query Wallet Balance

```csharp
// Create wallet API instance
var walletAPI = new WalletAPI(client);

// Check NEO balance for an address
string address = "NZNos2WqwVfNUXNj5VEqvvPzAqze3RXyP3";
uint neoBalance = await walletAPI.GetNeoBalanceAsync(address);
Console.WriteLine($"NEO Balance: {neoBalance}");

// Check GAS balance
decimal gasBalance = await walletAPI.GetGasBalanceAsync(address);
Console.WriteLine($"GAS Balance: {gasBalance}");
```

### Transfer NEP-17 Tokens

```csharp
// Create wallet API instance
var walletAPI = new WalletAPI(client);

// Transfer 10 GAS tokens
string privateKey = "your-private-key";
string toAddress = "NZNos2WqwVfNUXNj5VEqvvPzAqze3RXyP3";
string gasTokenHash = "0xd2a4cff31913016155e38e474a2c06d08be276cf"; // GAS token hash
decimal amount = 10;

// Perform the transfer
var tx = await walletAPI.TransferAsync(
    gasTokenHash, 
    privateKey, 
    toAddress, 
    amount
);

Console.WriteLine($"Transaction sent: {tx.Hash}");
```

### Invoke a Smart Contract

```csharp
// Get contract information
string contractHash = "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5";
var contractState = await client.GetContractStateAsync(contractHash);
Console.WriteLine($"Contract name: {contractState.Manifest.Name}");

// Invoke a read-only method
var result = await client.InvokeFunctionAsync(
    contractHash,
    "getTotalSupply",
    Array.Empty<RpcStack>()
);

Console.WriteLine($"Result: {result.Stack[0].Value}");
```

### Create and Submit Transaction

```csharp
// Create transaction manager factory
var factory = new TransactionManagerFactory(client);
    
// Create a transaction to invoke a contract method
byte[] script = new UInt160(contractHash).MakeScript("transfer", fromAccount, toAccount, amount);
var signers = new[] 
{ 
    new Signer 
    { 
        Account = fromAccount, 
        Scopes = WitnessScope.CalledByEntry 
    } 
};

// Build and sign the transaction
var manager = await factory.MakeTransactionAsync(script, signers);
Transaction tx = await manager
    .AddSignature(keyPair)
    .SignAsync();

// Submit the transaction
UInt256 txHash = await client.SendRawTransactionAsync(tx);
Console.WriteLine($"Transaction sent: {txHash}");
```

## Design Notes

- The library follows a modular architecture with specialized API classes for different blockchain operations
- Asynchronous methods are used throughout for non-blocking network operations
- Helper methods abstract complex blockchain operations into simple, intuitive calls
- The library handles serialization, deserialization, and error handling for RPC calls

## Relationship to Other Neo Components

RpcClient is a client-side library that communicates with Neo nodes running the RpcServer plugin. While it's located in the Plugins namespace for organizational purposes, it functions as a client SDK rather than a node plugin. This means:

- You don't need to run a Neo node to use this library
- It can connect to any Neo node that exposes an RPC endpoint
- It's designed to be included in standalone applications that need to interact with the Neo blockchain

## Requirements

- .NET 9.0 or higher
- Access to a Neo N3 blockchain node via RPC

## License

This project is licensed under the MIT License - see the LICENSE file in the main directory of the repository for details. 