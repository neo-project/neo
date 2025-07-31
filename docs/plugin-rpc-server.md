# Plugin RpcServer Documentation

This document provides a comprehensive reference for the plugin RpcServer.

## Table of Contents

1. [Get Started](#get-started)
1. [Node Methods](#node-methods)
2. [Blockchain Methods](#blockchain-methods)

---

## Get Started

### Install by `neo-cli`

1. **Start the `neo-cli`**: Just run `neo-cli` in the terminal.
2. **Download the Plugin**: Run `help install` to get help about how to install plugin.
3. **Configure the Plugin**: Create or modify the `RpcServer.json` configuration file in the `neo-cli` binary directory (`Plugins/RpcServer`) if needed.


### Compile Manually

1. **Clone the Repository**:
```bash
git clone https://github.com/neo-project/neo.git
cd neo
dotnet build
```
2. **Copy to `neo-cli` folder**: Copy the built plugin to the `neo-cli` binary directory.
3. **Create a `RpcServer.json` file**: Create or Copy the `RpcServer.json` file in `Plugins/RpcServer` directory according to the next section.
4. **Start the `neo-cli`**: Start/Restart `neo-cli` if needed.


### Configuration

Create or Copy the `RpcServer.json` file in `Plugins/RpcServer` directory:

```json
{
  "PluginConfiguration": {
    "UnhandledExceptionPolicy": "Ignore", // The unhandled exception policy, the default value is "Ignore"
    "Servers": [
      {
        "Network": 860833102, // The network ID
        "BindAddress": "127.0.0.1", // The bind address, 127.0.0.1 is the default value and for security reasons. 
        "Port": 10332, // The listening port
        "SslCert": "", // The SSL certificate, if want to use SSL, need to set the SSL certificate
        "SslCertPassword": "", // The SSL certificate password, if want to use SSL, you can set the password
        "TrustedAuthorities": [], // The trusted authorities, and if set, the RPC server will verify the certificate of the client
        "RpcUser": "", // The RPC user, if want to verify the RPC user and password, need to set the user
        "RpcPass": "", // The RPC password, if want to verify the RPC user and password, need to set the password
        "EnableCors": true, // Whether to enable CORS, if want to use CORS, need to set to true
        "AllowOrigins": [], // The allowed origins, if want to use CORS, need to set the allowed origins
        "KeepAliveTimeout": 60, // The keep alive timeout, the default value is 60 seconds
        "RequestHeadersTimeout": 15, // The request headers timeout, the default value is 15 seconds
        "MaxGasInvoke": 20, // The maximum gas invoke, the default value is 20 GAS
        "MaxFee": 0.1, // The maximum fee, the default value is 0.1 GAS
        "MaxConcurrentConnections": 40, // The maximum concurrent connections, the default value is 40
        "MaxIteratorResultItems": 100, // The maximum iterator result items, the default value is 100
        "MaxStackSize": 65535, // The maximum stack size, the default value is 65535
        "DisabledMethods": [ "openwallet" ], // The disabled methods, the default value is [ "openwallet" ]
        "SessionEnabled": false, // Whether to enable session, the default value is false
        "SessionExpirationTime": 60, // The session expiration time, the default value is 60 seconds
        "FindStoragePageSize": 50 // The find storage page size, the default value is 50
      }
    ]
  }
}
```

## Node Methods

### getconnectioncount
Gets the number of connections to the node.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getconnectioncount"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": 10 // The connected peers count
}
```

### getpeers
Gets information about the peers connected to the node.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getpeers"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "unconnected": [
      {"address": "The peer IP address", "port": "The port"}
    ],
    "bad": [],
    "connected": [
      {"address": "The peer IP address", "port": "The port"}
    ]
  }
}
```

### getversion
Gets version information about the node, including network, protocol, and RPC settings.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getversion"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tcpport": 10333,
    "nonce": 1,
    "useragent": "The user agent",
    "rpc": {
      "maxiteratorresultitems": 100, // The maximum number of items in the iterator result
      "sessionenabled": false // Whether the session is enabled
    },
    "protocol": {
      "addressversion": 0x35, // The address version
      "network": 5195086, // The network ID
      "validatorscount": 0, // The number of validators
      "msperblock": 15000, // The number of milliseconds per block
      "maxtraceableblocks": 2102400, // The maximum number of traceable blocks
      "maxvaliduntilblockincrement": 5760, // The maximum valid until block increment
      "maxtransactionsperblock": 512, // The maximum number of transactions per block
      "memorypoolmaxtransactions": 50000, // The maximum number of transactions in the memory pool
      "initialgasdistribution": 5200000000000000, // The initial gas distribution
      "hardforks": [
        {"name": "The hardfork name", "blockheight": 0} // The hardfork name and the block height
      ],
      "standbycommittee": ["The public key"], // The public keys of the standby committee
      "seedlist": ["The seed 'host:port' list"]
    }
  }
}
```

### sendrawtransaction
Sends a raw transaction to the network.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "sendrawtransaction",
  "params": ["A Base64 encoded transaction"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {"hash": "The hash of the transaction(UInt256)"}
}
```

### submitblock
Submits a new block to the network.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "submitblock",
  "params": ["A Base64 encoded block"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {"hash": "The hash of the block(UInt256)"}
}
```

---

## Blockchain Methods

### getbestblockhash
Gets the hash of the best (most recent) block.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getbestblockhash"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "The block hash(UInt256)"
}
```

### getblock
Gets a block by its hash or index.

**Request with block hash:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getblock",
  "params": ["The block hash(UInt256)"]
}
```

**Request with block index:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getblock",
  "params": [100] // The block index
}
```

**Request with block hash and verbose:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getblock",
  "params": ["The block hash(UInt256)", true] // The block hash and verbose is true
}
```

**Response (verbose=false):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "A base64-encoded string of the block"
}
```

**Response (verbose=true):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "hash": "The block hash(UInt256)",
    "size": 697, // The size of the block
    "version": 0, // The version of the block
    "previousblockhash": "The previous block hash(UInt256)",
    "merkleroot": "The merkle root(UInt256)",
    "time": 1627896461306, // The time of the block, unix timestamp in milliseconds
    "nonce": "09D4422954577BCE", // The nonce of the block
    "index": 100, // The index of the block
    "primary": 2, // The primary of the block
    "nextconsensus": "The Base58Check encoded next consensus address",
    "witnesses": [
      {"invocation": "A Base64 encoded string", "verification": "A Base64 encoded string"}
    ],
    "tx": [], // The transactions in the block
    "confirmations": 200, // The number of confirmations of the block
    "nextblockhash": "The next block hash(UInt256)" // The hash of the next block
  }
}
```

### getblockheadercount
Gets the number of block headers in the blockchain.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getblockheadercount"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": 100 // The number of block headers in the blockchain
}
```

### getblockcount
Gets the number of blocks in the blockchain.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getblockcount"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": 100 // The number of blocks in the blockchain
}
```

### getblockhash
Gets the hash of the block at the specified height.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getblockhash",
  "params": [100] // The block index
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "The block hash(UInt256)" // The hash of the block at the specified height
}
```

### getblockheader
Gets a block header by its hash or index.

**Request with block hash:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getblockheader",
  "params": ["The block hash(UInt256)"]
}
```

**Request with block index:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getblockheader",
  "params": [100] // The block index
}
```

**Request with block index and verbose:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getblockheader",
  "params": [100, true] // The block index and verbose is true
}
```

**Response (verbose=false):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "A base64-encoded string of the block header"
}
```

**Response (verbose=true):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "hash": "The block hash(UInt256)",
    "size": 696, // The size of the block header
    "version": 0, // The version of the block header
    "previousblockhash": "The previous block hash(UInt256)", // The hash of the previous block
    "merkleroot": "The merkle root(UInt256)", // The merkle root of the block header
    "time": 1627896461306, // The time of the block header, unix timestamp in milliseconds
    "nonce": "09D4422954577BCE", // The nonce of the block header
    "index": 100, // The index of the block header
    "primary": 2, // The primary of the block header
    "nextconsensus": "The Base58Check-encoded next consensus address", // The Base58Check-encoded next consensus address
    "witnesses": [
      {"invocation": "A Base64 encoded string", "verification": "A Base64 encoded string"}
    ],
    "confirmations": 200, // The number of confirmations of the block header
    "nextblockhash": "The next block hash(UInt256)" // The hash of the next block
  }
}
```

### getcontractstate
Gets the contract state by contract name, script hash, or ID.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getcontractstate",
  "params": ["Contract name, script hash, or the native contract id"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "A json string of the contract state"
}
```

### getrawmempool
Gets the current memory pool transactions.

**Request (shouldGetUnverified=false):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getrawmempool",
  "params": [false] // The shouldGetUnverified is false
}
```

**Request (shouldGetUnverified=true):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getrawmempool",
  "params": [true] // The shouldGetUnverified is true
}
```

**Response (shouldGetUnverified=false):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "verified": ["The tx hash(UInt256)"], // The verified transactions
  }
}
```

**Response (shouldGetUnverified=true):**

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "height": 100, // The height of the block
    "verified": ["The tx hash(UInt256)"], // The verified transactions
    "unverified": ["The tx hash(UInt256)"] // The unverified transactions
  }
}
```

### getrawtransaction
Gets a transaction by its hash.

**Request (verbose=true):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getrawtransaction",
  "params": ["The tx hash", true]
}
```

**Response (verbose=false):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "The Base64 encoded tx data"
}
```

**Response (verbose=true):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "hash": "The tx hash(UInt256)", // The hash of the transaction
    "size": 272, // The size of the transaction
    "version": 0, // The version of the transaction
    "nonce": 1553700339, // The nonce of the transaction
    "sender": "The Base58Check encoded sender address", // The Base58Check-encoded sender address
    "sysfee": "100000000", // The system fee of the transaction
    "netfee": "1272390", // The network fee of the transaction
    "validuntilblock": 2105487, // The valid until block of the transaction
    "attributes": [], // The attributes of the transaction
    "signers": [], // The signers of the transaction
    "script": "A Base64 encoded string", // The script of the transaction
    "witnesses": [
      {"invocation": "A Base64 encoded string", "verification": "A Base64 encoded string"}
    ],
    "confirmations": 100, // The number of confirmations of the transaction
    "blockhash": "The block hash(UInt256)", // The hash of the block
    "blocktime": 1627896461306 // The time of the block, unix timestamp in milliseconds
  }
}
```

### getstorage
Gets the storage item by contract ID or script hash and key.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getstorage",
  "params": ["The contract id(int), hash(UInt160), or native contract name(string)", "The Base64 encoded key"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "The Base64 encoded storage value"
}
```

### findstorage
Lists storage items by contract ID or script hash and prefix.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "findstorage",
  "params": [
    "The contract id(int), hash(UInt160), or native contract name(string)",
    "The Base64 encoded key prefix", // The Base64 encoded key prefix
    0 // The start index, optional
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "truncated": true, // Whether the results are truncated
    "next": 100, // The next index
    "results": [
      {"key": "The Base64 encoded storage key", "value": "The Base64 encoded storage value"}, // The storage item
      {"key": "The Base64 encoded storage key", "value": "The Base64 encoded storage value"} // The storage item
      // ...
    ]
  }
}
```

### gettransactionheight
Gets the height of a transaction by its hash.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "gettransactionheight",
  "params": ["The tx hash(UInt256)"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": 100 // The height of the transaction
}
```

### getnextblockvalidators
Gets the next block validators.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getnextblockvalidators"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": [
    {
      "publickey": "The public key", // The public key of the validator
      "votes": 100 // The votes of the validator
    }
    // ...
  ]
}
```

### getcandidates
Gets the list of candidates for the next block validators.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getcandidates"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": [
    {
      "publickey": "The public key", // The public key of the candidate
      "votes": 100, // The votes of the candidate
      "active": true // Is active or not
    }
    // ...
  ]
}
```

### getcommittee
Gets the list of committee members.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getcommittee"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": ["The public key"] // The public keys of the committee
}
```

### getnativecontracts
Gets the list of native contracts.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getnativecontracts"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": [
    {
      "id": -1, // The contract id
      "updatecounter": 0, // The update counter
      "hash": "The contract hash(UInt160)", // The contract hash
      "nef": {
        "magic": 0x3346454E, // The magic number, always 0x3346454E at present.
        "compiler": "The compiler name",
        "source": "The url of the source file",
        "tokens": [
          {
            "hash": "The token hash(UInt160)",
            "method": "The token method name",
            "paramcount": 0, // The number of parameters
            "hasreturnvalue": false, // Whether the method has a return value
            "callflags": 0 // see CallFlags
          } // A token in the contract
          // ...
        ],
        "script": "The Base64 encoded script", // The Base64 encoded script
        "checksum": 0x12345678 // The checksum
      },
      "manifest": {
        "name": "The contract name",
        "groups": [
          {"pubkey": "The public key", "signature": "The signature"} // A group in the manifest
        ],
        "features": {}, // The features that the contract supports
        "supportedstandards": ["The standard name"], // The standards that the contract supports
        "abi": {
          "methods": [
            {
              "name": "The method name",
              "parameters": [
                {"name": "The parameter name", "type": "The parameter type"} // A ContractParameter in the method
                // ...
              ],
              "returntype": "The return type",
              "offset": 0, // The offset in script of the method
              "safe": false // Whether the method is safe
            } // A method in the abi
            // ...
          ],
          "events": [
            {
              "name": "The event name",
              "parameters": [
                {"name": "The parameter name", "type": "The parameter type"} // A ContractParameter in the event
                // ...
              ]
            } // An event in the abi
            // ...
          ]
        }, // The abi of the contract
        "permissions": [
          {
            "contract": "The contract hash(UInt160), group(ECPoint), or '*'", // '*' means all contracts
            "methods": ["The method name or '*'"] // '*' means all methods
          } // A permission in the contract
          // ...
        ], // The permissions of the contract
        "trusts": [
          {
            "contract": "The contract hash(UInt160), group(ECPoint), or '*'", // '*' means all contracts
            "methods": ["The method name or '*'"] // '*' means all methods
          } // A trust in the contract
          // ...
        ], // The trusts of the contract
        "extra": {} // A json object, the extra content of the contract
      } // The manifest of the contract
    }
  ]
}
```
