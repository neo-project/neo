# Plugin RpcServer Documentation

This document provides a comprehensive reference for the plugin RpcServer.
Including how to enable RPC server, and RPC method definitions from RpcServer plugin and other plugins.

## Table of Contents

1. [Get Started](#get-started)
1. [Node Methods](#node-methods)
2. [Blockchain Methods](#blockchain-methods)
3. [Smart Contract Methods](#smart-contract-methods)
4. [Wallet Methods](#wallet-methods)
5. [Utility Methods](#utility-methods)
6. [RpcMethods from other Plugins](#rpcmethods-from-other-plugins)

---

## Get Started

### Install by `neo-cli`

1. **Start the `neo-cli`**: Just run `neo-cli` in the terminal.
2. **Download the Plugin**: Run `help install` to get help about how to install plugin.
3. **Configure the Plugin**: Create or modify the `RpcServer.json` configuration file in the `neo-cli` binary directory (`Plugins/RpcServer`) if needed. 
If want to use RPC methods from other plugins, need to enable the plugin first.


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
    "hash": "The block hash(UInt256)", // Hex-encoded UInt256 with 0x prefix
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

---

## Smart Contract Methods

### invokefunction
Invokes a function on a smart contract.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "invokefunction",
  "params": [
    "The script hash(UInt160)",
    "The operation to invoke as a string",
    [
      {
        "type": "ContractParameterType", // The type of the parameter, see ContractParameterType
        "value": "The parameter value" // The value of the parameter
      } // A parameter in the operation
      // ...
    ], // The parameters of the operation, optional(can be null)
    [
      {
        // The part of the Signer
        "account": "An UInt160 or Base58Check address", // The account of the signer, required
        "scopes": "WitnessScope", // The scopes of the signer, see WitnessScope, required
        "allowedcontracts": ["The contract hash(UInt160)"], // The allowed contracts of the signer, optional
        "allowedgroups": ["PublicKey"], // The allowed groups of the signer, optional
        "rules": [
          {
            "action": "WitnessRuleAction", // The action of the witness rule, see WitnessRuleAction
            "condition": { /* A json of WitnessCondition */ } // The condition of the witness rule, see WitnessCondition
          } // A rule in the witness
          // ...
        ], // WitnessRule array, optional(can be null)

        // The part of the Witness
        "invocation": "A Base64 encoded string", // The invocation of the witness, optional
        "verification": "A Base64 encoded string" // The verification of the witness, optional
      }
    ], // The signers and witnesses list, optional(can be null)
    false // useDiagnostic, a bool value indicating whether to use diagnostic information, optional
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "script": "A Base64 encoded script",
    "state": "A string of VMState", // see VMState
    "gasconsumed": "An integer number in string", // The gas consumed
    "exception": "The exception message", // The exception message
    "stack": [
      {"type": "The stack item type(StackItemType)", "value": "The stack item value"} // A StackItem in the stack
      // ...
    ],
    "notifications": [
      {
        "eventname": "The event name", // The name of the event
        "contract": "The contract hash", // The hash of the contract
        "state": {"interface": "A string", "id": "The GUID string"} // The state of the event
      }
    ],
    "diagnostics": {
      "invokedcontracts": {"hash": "The contract hash", "call": [{"hash": "The contract hash"}]}, // The invoked contracts
      "storagechanges": [
        {
          "state": "The TrackState string", // The type of the state, see TrackState
          "key": "The Base64 encoded key", // The key of the storage change
          "value": "The Base64 encoded value" // The value of the storage change
        } // A storage change
        // ...
      ] // The storage changes
    }, // The diagnostics, optional
    "session": "A GUID string" // The session id, optional
  }
}
```

### invokescript
Invokes a script.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "invokescript",
  "params": [
    "A Base64 encoded script",
    [
      {
        // The part of the Signer
        "account": "An UInt160 or Base58Check address", // The account of the signer, required
        "scopes": "WitnessScope", // The scopes of the signer, see WitnessScope, required
        "allowedcontracts": ["The contract hash(UInt160)"], // The allowed contracts of the signer, optional
        "allowedgroups": ["PublicKey"], // The allowed groups of the signer, optional
        "rules": [
          {
            "action": "WitnessRuleAction", // The action of the witness rule, see WitnessRuleAction
            "condition": { /* A json of WitnessCondition */ } // The condition of the witness rule, see WitnessCondition
          } // A rule in the witness
          // ...
        ], // WitnessRule array, optional(can be null)

        // The part of the Witness
        "invocation": "A Base64 encoded string", // The invocation of the witness, optional
        "verification": "A Base64 encoded string" // The verification of the witness, optional
      }
    ], // The signers and witnesses list, optional(can be null)
    false // useDiagnostic, a bool value indicating whether to use diagnostic information, optional
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "script": "A Base64 encoded string",
    "state": "A string of VMState",
    "gasconsumed": "An integer number in string",
    "exception": "The exception message",
    "stack": [
      {"type": "The stack item type(StackItemType)", "value": "The stack item value"}
    ],
    "notifications": [
      {
        "eventname": "The event name",
        "contract": "The contract hash",
        "state": {"interface": "A string", "id": "The GUID string"}
      }
    ],
    "diagnostics": {
      "invokedcontracts": {"hash": "The contract hash", "call": [{"hash": "The contract hash"}]},
      "storagechanges": [{"state": "The state", "key": "The key", "value": "The value"}]
    },
    "session": "A GUID string"
  }
}
```

### traverseiterator
Traverses an iterator to get more items.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "traverseiterator",
  "params": [
    "A GUID string(The session id)",
    "A GUID string(The iterator id)",
    100 // An integer number(The number of items to traverse)
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": [
    {"type": "The stack item type(StackItemType)", "value": "The stack item value"}
  ]
}
```

### terminatesession
Terminates a session.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "terminatesession",
  "params": ["A GUID string(The session id)"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": true // true if the session is terminated successfully, otherwise false
}
```

### getunclaimedgas
Gets the unclaimed gas of an address.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getunclaimedgas",
  "params": ["An UInt160 or Base58Check address"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {"unclaimed": "An integer in string", "address": "The Base58Check encoded address"}
}
```

---

## Wallet Methods

### closewallet
Closes the currently opened wallet.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "closewallet",
  "params": []
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": true
}
```

### dumpprivkey
Exports the private key of a specified address.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "dumpprivkey",
  "params": ["An UInt160 or Base58Check address"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "A WIF-encoded private key"
}
```

### getnewaddress
Creates a new address in the wallet.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getnewaddress",
  "params": []
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "The newly created address" // Base58Check address
}
```

### getwalletbalance
Gets the balance of a specified asset in the wallet.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getwalletbalance",
  "params": ["An UInt160 address"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {"balance": "0"} // An integer number in string, the balance of the specified asset in the wallet
}
```

### getwalletunclaimedgas
Gets the amount of unclaimed GAS in the wallet.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getwalletunclaimedgas",
  "params": []
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "The amount of unclaimed GAS(an integer number in string)"
}
```

### importprivkey
Imports a private key into the wallet.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "importprivkey",
  "params": ["A WIF-encoded private key"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "address": "The Base58Check address",
    "haskey": true,
    "label": "The label",
    "watchonly": false
  }
}
```

### calculatenetworkfee
Calculates the network fee for a given transaction.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "calculatenetworkfee",
  "params": ["A Base64 encoded transaction"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {"networkfee": "The network fee(an integer number in string)"}
}
```

### listaddress
Lists all addresses in the wallet.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "listaddress",
  "params": []
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": [
    {"address": "address", "haskey": true, "label": "label", "watchonly": false}
  ]
}
```

### openwallet
Opens a wallet file.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "openwallet",
  "params": ["path", "password"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": true
}
```

### sendfrom
Transfers an asset from a specific address to another address.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "sendfrom",
  "params": [
    "An UInt160 assetId",
    "An UInt160 from address",
    "An UInt160 to address",
    "An amount as a string(An integer/decimal number in string)",
    ["UInt160 or Base58Check address"] // signers, optional(can be null)
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "hash": "The tx hash(UInt256)", // The hash of the transaction
    "size": 272, // The size of the transaction
    "version": 0, // The version of the transaction
    "nonce": 1553700339, // The nonce of the transaction
    "sender": "The Base58Check address", // The sender of the transaction
    "sysfee": "100000000", // The system fee of the transaction
    "netfee": "1272390", // The network fee of the transaction
    "validuntilblock": 2105487, // The valid until block of the transaction
    "attributes": [], // The attributes of the transaction
    "signers": [{"account": "The UInt160 address", "scopes": "CalledByEntry"}], // The signers of the transaction
    "script": "A Base64 encoded script",
    "witnesses": [{"invocation": "A Base64 encoded string", "verification": "A Base64 encoded string"}] // The witnesses of the transaction
  }
}
```

### sendmany
Transfers assets to multiple addresses.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "sendmany",
  "params": [
    "An UInt160 address", // "from", optional(can be null)
    [
      {
        "asset": "An UInt160 assetId",
        "value": "An integer/decimal as a string",
        "address": "An UInt160 address"
      }
      // ...
    ], // The transfers list, optional(can be null)
    ["UInt160 or Base58Check address"] // signers, optional(can be null)
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "hash": "The tx hash(UInt256)", // The hash of the transaction
    "size": 483, // The size of the transaction
    "version": 0, // The version of the transaction
    "nonce": 34429660, // The nonce of the transaction
    "sender": "The Base58Check address", // The sender of the transaction
    "sysfee": "100000000", // The system fee of the transaction
    "netfee": "2483780", // The network fee of the transaction
    "validuntilblock": 2105494, // The valid until block of the transaction
    "attributes": [], // The attributes of the transaction
    "signers": [{"account": "The UInt160 address", "scopes": "CalledByEntry"}], // The signers of the transaction
    "script": "A Base64 encoded script",
    "witnesses": [{"invocation": "A Base64 encoded string", "verification": "A Base64 encoded string"}] // The witnesses of the transaction
  }
}
```

### sendtoaddress
Transfers an asset to a specific address.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "sendtoaddress",
  "params": [
    "An UInt160 assetId",
    "An UInt160 address(to)",
    "An amount as a string(An integer/decimal number)"
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "hash": "The tx hash(UInt256)", // The hash of the transaction
    "size": 483, // The size of the transaction
    "version": 0, // The version of the transaction
    "nonce": 34429660, // The nonce of the transaction
    "sender": "The Base58Check address", // The sender of the transaction
    "sysfee": "100000000", // The system fee of the transaction
    "netfee": "2483780", // The network fee of the transaction
    "validuntilblock": 2105494, // The valid until block of the transaction
    "attributes": [], // The attributes of the transaction
    "signers": [
      {
        "account": "The UInt160 address",
        "scopes": "CalledByEntry" // see WitnessScope
      }
      // ...
    ], // The signers of the transaction
    "script": "A Base64 encoded script", // The script of the transaction
    "witnesses": [{"invocation": "A Base64 encoded string", "verification": "A Base64 encoded string"}] // The witnesses of the transaction
  }
}
```

### canceltransaction
Cancels an unconfirmed transaction.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "canceltransaction",
  "params": [
    "An tx hash(UInt256)",
    ["UInt160 or Base58Check address"], // signers, optional(can be null)
    "An amount as a string(An integer/decimal number)" // extraFee, optional
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "hash": "The tx hash(UInt256)", // The hash of the transaction
    "size": 483, // The size of the transaction
    "version": 0, // The version of the transaction
    "nonce": 34429660, // The nonce of the transaction
    "sender": "The Base58Check address", // The sender of the transaction
    "sysfee": "100000000", // The system fee of the transaction
    "netfee": "2483780", // The network fee of the transaction
    "validuntilblock": 2105494, // The valid until block of the transaction
    "attributes": [], // The attributes of the transaction
    "signers": [{"account": "The UInt160 address", "scopes": "CalledByEntry"}], // The signers of the transaction
    "script": "A Base64 encoded script",
    "witnesses": [{"invocation": "A Base64 encoded string", "verification": "A Base64 encoded string"}] // The witnesses of the transaction
  }
}
```

### invokecontractverify
Invokes the verify method of a contract.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "invokecontractverify",
  "params": [
    "The script hash(UInt160)",
    [
      {
        "type": "The type of the parameter",
        "value": "The value of the parameter"
      }
      // ...
    ], // The arguments as an array of ContractParameter JSON objects
    [
      {
        // The part of the Signer
        "account": "An UInt160 or Base58Check address", // The account of the signer, required
        "scopes": "WitnessScope", // The scopes of the signer, see WitnessScope, required
        "allowedcontracts": ["The contract hash(UInt160)"], // The allowed contracts of the signer, optional
        "allowedgroups": ["PublicKey"], // The allowed groups of the signer, optional
        "rules": [
          {
            "action": "WitnessRuleAction", // The action of the witness rule, see WitnessRuleAction
            "condition": { /* A json of WitnessCondition */ } // The condition of the witness rule, see WitnessCondition
          } // A rule in the witness
          // ...
        ], // WitnessRule array, optional(can be null)

        // The part of the Witness
        "invocation": "A Base64 encoded string", // The invocation of the witness, optional
        "verification": "A Base64 encoded string" // The verification of the witness, optional
      }
      // ...
    ] // The signers and witnesses as an array of JSON objects
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "script": "A Base64 encoded string",
    "state": "A string of VMState",
    "gasconsumed": "An integer number in string",
    "exception": "The exception message",
    "stack": [{"type": "The stack item type", "value": "The stack item value"}]
  }
}
```

## Utility Methods

### listplugins
Lists all plugins.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "listplugins"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": [
    {"name": "The plugin name", "version": "The plugin version", "interfaces": ["The plugin method name"]}
  ]
}
```

### validateaddress
Validates an address.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "validateaddress",
  "params": ["The Base58Check address"]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {"address": "The Base58Check address", "isvalid": true}
}
```

# RpcMethods from other Plugins

## Plugin: ApplicationLogs

### getppplicationlog
Gets the block or the transaction execution log. The execution logs are stored if the ApplicationLogs plugin is enabled.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getppplicationlog",
  "params": [
    "The block hash or the transaction hash(UInt256)", // Hex-encoded UInt256 with 0x prefix
    "The trigger type(string)" // The trigger type, optional, default is "" and means no filter trigger type. It can be "OnPersist", "PostPersist", "Verification", "Application", "System" or "All"(enum TriggerType). If want to filter by trigger type, need to set the trigger type.
  ]
}
```

**Response:**
If the block hash is provided, the response is a block execution log.
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "blockhash": "The block hash(UInt256)", // Hex-encoded UInt256 with 0x prefix
    "executions": [ // The execution logs of OnPersist or PostPersist
      {
        "trigger": "The trigger type(string)", // see TriggerType
        "vmstate": "The VM state(string)", // see VMState
        "gasconsumed": "The gas consumed(number in string)",
        "stack": [{"type": "The stack item type", "value": "The stack item value"}], // The stack of the execution, optional. No stack if get stack failed.
        "exception": "The exception message", // The exception message if get stack failed, optional
        "notifications": [
          {
            "contract": "The contract hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
            "eventname": "The event name",
            "state": { //  Object if the state or 'error: recursive reference' if get state failed.
              "type": "Array", // always "Array" now.
              "value": [
                {
                  "type": "The stack item type", // see StackItemType
                  "value": "The stack item value" // see StackItem, maybe Integer, Boolean, String, Array, Map, etc.
                }
                // ...
              ]
            }
          }
          // ...
        ],
        "logs": [ // The logs of the execution, optional. Only Debug option is enabled, the logs will be returned.
          {
            "contract": "The contract hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
            "message": "The message"
          }
          // ...
        ]
      }
      // ...
    ] // The execution logs of OnPersist or PostPersist
  }
}
```

If the transaction hash is provided, the response is a transaction execution log.
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "txid": "The transaction hash(UInt256)", // Hex-encoded UInt256 with 0x prefix
    "executions": [ // The execution log of Verification or Application
      {
        "trigger": "The trigger type(string)", // see TriggerType
        "vmstate": "The VM state(string)", // see VMState
        "gasconsumed": "The gas consumed(number in string)",
        "stack": [{"type": "The stack item type", "value": "The stack item value"}], // The stack of the execution, optional. No stack if get stack failed.
        "exception": "The exception message", // The exception message if get stack failed, optional
        "notifications": [
          {
            "contract": "The contract hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
            "eventname": "The event name",
            "state": { //  Object if the state or 'error: recursive reference' if get state failed.
              "type": "Array", // always "Array" now.
              "value": [
                {
                  "type": "The stack item type", // see StackItemType
                  "value": "The stack item value" // see StackItem, maybe Integer, Boolean, String, Array, Map, etc.
                }
                // ...
              ]
            }
          }
          // ...
        ],
        "logs": [ // The logs of the execution, optional. Only Debug option is enabled, the logs will be returned.
          {
            "contract": "The contract hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
            "message": "The message"
          }
          // ...
        ]
      }
      // ...
    ] // The execution log of Verification or Application
  }
}
```

## Plugin: OracleService

### submitoracleresponse
Submits the oracle response of an Oracle request.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "submitoracleresponse",
  "params": [
    "The oracle public key(byte[])", // Base64-encoded if access from json-rpc
    "The request id(ulong)", // The Oracle request id
    "The transaction signature(byte[])", // Base64-encoded if access from json-rpc
    "The message signature(byte[])" // Base64-encoded if access from json-rpc
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {} // Empty object if success
}
```

## Plugin: StateService

### getstateroot
Gets the state root by index.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getstateroot",
  "params": [
    1 // It's an uint number, the index of the state root
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "version": 0, // A byte number, the version of the state root
    "index": 1, // An uint number, the index of the state root
    "roothash": "The state root hash(UInt256)", // Hex-encoded UInt256 with 0x prefix
    "witnesses": [{"invocation": "A Base64 encoded string", "verification": "A Base64 encoded string"}]
  }
}
```

### getproof
Gets the proof of a key

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getproof",
  "params": [
    "The state root hash(UInt256)", // Hex-encoded UInt256 with 0x prefix
    "The contract hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
    "The key(Base64-encoded string)" // The key of the storage
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "The proof(Base64-encoded string)" // var-bytes storage-key + var-int proof-count + var-bytes proof-item
}
```

### verifyproof
Verifies the proof of a key

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "verifyproof",
  "params": [
    "The state root hash(UInt256)", // Hex-encoded UInt256 with 0x prefix
    "The proof(Base64-encoded string)" // var-bytes storage-key + var-int proof-count + var-bytes proof-item
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "The verify result(Base64-encoded string)"
}
```

### getstateheight
Gets the current state height information

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getstateheight"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "localrootindex": 1, // An uint number, optional, the index of the local state root
    "validatedrootindex": 1 // An uint number, optional, the index of the validated state root
  }
}
```

### findstates
List the states of a key prefix

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "findstates",
  "params": [
    "The state root hash(UInt256)", // Hex-encoded UInt256 with 0x prefix
    "The contract hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
    "The key prefix(Base64-encoded string)", // The key prefix of the storage
    "The key(Base64-encoded string)", // The key of the storage
    "The count(int)" // The count of the results, If not set or greater than the MaxFindResultItems, the MaxFindResultItems will be used
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "firstproof": "The proof of the first key(Base64-encoded string)", // Optional, if the results are not empty, the proof of the first key will be returned
    "lastproof": "The proof of the last key(Base64-encoded string)", // Optional, if the results length is greater than 1, the proof of the last key will be returned
    "truncated": true, // Whether the results are truncated
    "results": [
      {"key": "The key(Base64-encoded string)", "value": "The value(Base64-encoded string)"} // The key-value pair of the state
    ]
  }
}
```

### getstate
Gets the state of a key

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getstate",
  "params": [
    "The state root hash(UInt256)", // Hex-encoded UInt256 with 0x prefix
    "The contract hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
    "The key(Base64-encoded string)" // The key of the state
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "The state value(Base64-encoded string)" // The value of the state
}
```

## Plugin: TokensTracker

### getnep11transfers
Gets the transfers of NEP-11 token

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getnep11transfers",
  "params": [
    "The address(Address)", // UInt160 or Base58Check-encoded address
    0, // It's an ulong number, the unix timestamp in milliseconds, optional, default to 1 week ago
    0 // It's an ulong number, the unix timestamp in milliseconds, optional, default to now
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "address": "The address(Address)", // UInt160 or Base58Check-encoded address
    "sent": [
      {
        "tokenid": "The token id(Hex-encoded string)",
        "timestamp": 123000, // The unix timestamp in milliseconds
        "assethash": "The asset hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
        "transferaddress": "The transfer address(UInt160)", // The address of the transfer, null if no transfer address
        "amount": "The amount(integer number in string)",
        "blockindex": 123, // The block index
        "transfernotifyindex": 123, // The transfer notify index
        "txhash": "The transaction hash(UInt256)" // Hex-encoded UInt256 with 0x prefix
      }
      // ...
    ],
    "received": [
      {
        "tokenid": "The token id(Hex-encoded string)",
        "timestamp": 123000, // The unix timestamp in milliseconds
        "assethash": "The asset hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
        "transferaddress": "The transfer address(UInt160)", // The address of the transfer, null if no transfer address
        "amount": "The amount(integer number in string)",
        "blockindex": 123, // The block index
        "transfernotifyindex": 123, // The transfer notify index
        "txhash": "The transaction hash(UInt256)" // Hex-encoded UInt256 with 0x prefix
      }
      // ...
    ]
  }
}
```

### getnep11balances
Gets the balances of NEP-11 token

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getnep11balances",
  "params": [
    "The address(Address)" // UInt160 or Base58Check-encoded address
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "address": "The address", 
    "balance": [
      {
        "assethash": "The asset hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
        "name": "The name(string)",
        "symbol": "The symbol(string)",
        "decimals": "The decimals(integer number in string)",
        "tokens": [
          {
            "tokenid": "The token id(Hex-encoded string)",
            "amount": "The amount(integer number in string)",
            "lastupdatedblock": 123 // The block index
          }
          // ...
        ]
      }
      // ...
    ]
  }
}
```

### getnep11properties
Gets the properties of NEP-11 token

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getnep11properties",
  "params": [
    "The address(Address)", // UInt160 or Base58Check-encoded address
    "The token id(Hex-encoded string)" // The token id of the NEP-11 token
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    // The properties of the NEP-11 token
  }
}
```

### getnep17transfers
Gets the transfers of NEP-17 token

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getnep17transfers",
  "params": [
    "The address(Address)", // UInt160 or Base58Check-encoded address
    0, // It's an ulong number, the unix timestamp in milliseconds, optional, default to 1 week ago
    0 // It's an ulong number, the unix timestamp in milliseconds, optional, default to now
  ]
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "address": "The address(Address)", // UInt160 or Base58Check-encoded address
    "sent": [
      {
        "tokenid": "The token id(Hex-encoded string)",
        "timestamp": 123000, // The unix timestamp in milliseconds
        "assethash": "The asset hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
        "transferaddress": "The transfer address(UInt160)", // The address of the transfer, null if no transfer address
        "amount": "The amount(integer number in string)",
        "blockindex": 123, // The block index
        "transfernotifyindex": 123, // The transfer notify index
        "txhash": "The transaction hash(UInt256)" // Hex-encoded UInt256 with 0x prefix
      }
      // ...
    ],
    "received": [
      {
        "tokenid": "The token id(Hex-encoded string)",
        "timestamp": 123000, // The unix timestamp in milliseconds
        "assethash": "The asset hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
        "transferaddress": "The transfer address(UInt160)", // The address of the transfer, null if no transfer address
        "amount": "The amount(integer number in string)",
        "blockindex": 123, // The block index
        "transfernotifyindex": 123, // The transfer notify index
        "txhash": "The transaction hash(UInt256)" // Hex-encoded UInt256 with 0x prefix
      }
      // ...
    ]
  }
}
```

### getnep17balances
Gets the balances of NEP-17 token

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "getnep17balances",
  "params": [
    "The address(Address)" // UInt160 or Base58Check-encoded address
  ]
}
```
**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "address": "The address(Address)", // UInt160 or Base58Check-encoded address
    "balance": [
      {
        "assethash": "The asset hash(UInt160)", // Hex-encoded UInt160 with 0x prefix
        "name": "The name(string)",
        "symbol": "The symbol(string)",
        "decimals": "The decimals(integer number in string)",
        "amount": "The amount(integer number in string)",
        "lastupdatedblock": 123 // The block index
      }
      // ...
    ]
  }
}
```
