# Native Contracts API

Native contracts are the contracts that are implemented in the Neo blockchain, and native contract api are the methods that are provided by the native contracts.

## Table of Contents

1. [How to Call Native Contract Methods](#how-to-call-native-contract-methods)
2. [Contract Management](#contract-management)
3. [StdLib](#stdlib)
4. [CryptoLib](#cryptolib)
5. [LedgerContract](#ledgercontract)
6. [NeoToken](#neotoken)
7. [GasToken](#gastoken)
8. [PolicyContract](#policycontract)
9. [RoleManagement](#rolemanagement)
10. [OracleContract](#oraclecontract)
11. [Notary](#notary)

---

## How to Call Native Contract Methods

When implementing a smart contract, this contract can call the methods of the native contracts by classes in `Neo.SmartContract.Framework.Native` namespace(`ContractManagement`, `StdLib`, `CryptoLib`, `LedgerContract`, `NeoToken`, `GasToken`, `PolicyContract`, `RoleManagement`, `OracleContract`, `Notary`, etc.).

When building a transaction script, the script can call the methods of the native contracts by creating a contract call operation in the script(see `ScriptBuilder.EmitDynamicCall`).

When calling a native contract method, there are several tips and notes:
1. Native contract uses lowerCamelCase naming style for method name. For example, the `Transfer` method of `NeoToken` and `GasToken` is called `transfer`.
2. Most of native contract methods require pay CPU fee. Few native contract methods require pay Storage fee(In the unit of datoshi, 1 datoshi = 1e-8 GAS).
3. A part of native contract methods require CallFlags. If no such CallFlags is provided, the call will be failed. For example, the `Transfer` method of `NeoToken` and `GasToken` requires `CallFlags.States | CallFlags.AllowCall | CallFlags.AllowNotify` flags, and if a transaction script calls this method, it must provide these flags, otherwise the transaction execution will be failed.
4. Some native contract methods are only allowed to be called before or after a certain hardfork. For example, the `OnNEP17Payment` method of `NeoToken` is only allowed to be called after the `HF_Echidna` hardfork.
5. A native contract method may have different behaviors in different hardforks. For example, the `RegisterCandidate` in `NeoToken` has different CallFlags before and after the `HF_Echidna` hardfork, and the `GetAttributeFee` method in `PolicyContract` has different behaviors before  and after the `HF_Echidna` hardfork.


## Contract Management
|   Method  |  Description  |  Parameters  |  Return Value  |  CPU fee  |  Storage fee  |  Call Flags  |  Hardfork  |
|-----------|---------------|--------------|----------------|-----------|---------------|--------------|--------------|
| getMinimumDeploymentFee | Gets the minimum deployment fee. | -- | Integer(*fee*) | 1<<15 | 0 | ReadStates | -- |
| setMinimumDeploymentFee | Sets the minimum deployment fee. Only committee members can call this method. | Integer(*fee*) | -- | 1<<15 | 0 | States | -- |
| getContract | Gets the deployed contract with the specified hash. | Hash160(*contract hash*) | ContractState | 1<<15 | 0 | ReadStates | -- |
| isContract | Checks if exists the deployed contract with the specified hash. | Hash160(*contract hash*) | Boolean | 1<<14 | 0 | ReadStates | HF_Echidna |
| getContractById | Gets the deployed contract with the specified ID. | Integer(*contract ID*) | ContractState | 1<<15 | 0 | ReadStates | -- |
| listContracts | Gets all deployed contracts. | -- | Iterator(/*key: ContractID, value: ContractState*/) | 1<<15 | 0 | ReadStates | -- |
| hasMethod | Checks if a method exists in a contract. | Hash160(*contract hash*), String(*method name*), Integer(*number of parameters*) | Boolean | 1<<15 | 0 | ReadStates | -- |
| deploy | Deploys a new contract. | Bytes(*NEF file*), Byte(*manifest*) | ContractState | 1<<15 | 0 | States,AllowNotify(before HF_Aspidochelone), or All(after HF_Aspidochelone) | -- |
| deploy | Deploys a new contract. | Bytes(*NEF file*), Byte(*manifest*), StackItem(*data*) | ContractState | 1<<15 | 0 | States,AllowNotify(before HF_Aspidochelone), or All(after HF_Aspidochelone) | -- |
| update | Updates an existing contract. | Bytes(*NEF file*), Byte(*manifest*) | -- | 1<<15 | 0 | States,AllowNotify(before HF_Aspidochelone), or All(after HF_Aspidochelone) | -- |
| update | Updates an existing contract. | Bytes(*NEF file*), Byte(*manifest*), StackItem(*data*) | -- | 1<<15 | 0 | States,AllowNotify(before HF_Aspidochelone), or All(after HF_Aspidochelone) | -- |
| destroy | Destroys an existing contract. | -- | -- | 1<<15 | 0 | States,AllowNotify | -- |



## StdLib
|   Method  |  Description  |  Parameters  |  Return Value  |  CPU fee  |  Storage fee  |  Call Flags  |  Hardfork  |
|-----------|---------------|--------------|----------------|-----------|---------------|--------------|--------------|
| serialize | Serializes a StackItem to a byte array. | StackItem(*item*) | Bytes(*serialized item*) | 1<<12 | 0 | -- | -- |
| deserialize | Deserializes a byte array to a StackItem. | Bytes(*data*) | StackItem(*item*) | 1<<14 | 0 | -- | -- |
| jsonSerialize | Serializes a StackItem to a JSON string. | StackItem(*item*) | String(*json*) | 1<<12 | 0 | -- | -- |
| jsonDeserialize | Deserializes a JSON string to a StackItem. | String(*json*) | StackItem(*item*) | 1<<14 | 0 | -- | -- |
| itoa | Converts an integer to a string. | Integer(*value*) | String(*string*) | 1<<12 | 0 | -- | -- |
| itoa | Converts an integer to a string. | Integer(*value*), Integer(*base, 10 or 16*) | String(*string*) | 1<<12 | 0 | -- | -- |
| atoi | Converts a string to an integer. | String(*value*) | Integer(*integer*) | 1<<6 | 0 | -- | -- |
| atoi | Converts a string to an integer. | String(*value*), Integer(*base, 10 or 16*) | Integer(*integer*) | 1<<6 | 0 | -- | -- |
| base64Encode | Encodes a byte array to a base64 string. | Bytes(*data*) | String(*base64*) | 1<<5 | 0 | -- | -- |
| base64Decode | Decodes a base64 string to a byte array. | String(*base64*) | Bytes(*data*) | 1<<5 | 0 | -- | -- |
| base64UrlEncode | Encodes a string to a base64url string. | String(*data*) | String(*base64url*) | 1<<5 | 0 | -- | HF_Echidna |
| base64UrlDecode | Decodes a base64url string to a string. | String(*base64url*) | String(*string*) | 1<<5 | 0 | -- | HF_Echidna |
| base58Encode | Encodes a byte array to a base58 string. | Bytes(*data*) | String(*base58*) | 1<<13 | 0 | -- | -- |
| base58Decode | Decodes a base58 string to a byte array. | String(*base58*) | Bytes(*data*) | 1<<10 | 0 | -- | -- |
| base58CheckEncode | Encodes a byte array to a base58Check string. | Bytes(*data*) | String(*base58Check*) | 1<<16 | 0 | -- | -- |
| base58CheckDecode | Decodes a base58Check string to a byte array. | String(*base58Check*) | Bytes(*data*) | 1<<16 | 0 | -- | -- |
| hexEncode | Encodes a byte array to a hex string. | Bytes(*data*) | String(*hex*) | 1<<5 | 0 | -- | HF_Faun |
| hexDecode | Decodes a hex string to a byte array. | String(*hex*) | Bytes(*data*) | 1<<5 | 0 | -- | HF_Faun |
| memoryCompare | Compares two byte arrays. | Bytes(*str1*), Bytes(*str2*) | Integer(*result*) | 1<<5 | 0 | -- | -- |
| memorySearch | Searches for a byte array in a byte array. | Bytes(*mem*), Bytes(*value*) | Integer(*index*) | 1<<6 | 0 | -- | -- |
| memorySearch | Searches for a byte array in a byte array. | Bytes(*mem*), Bytes(*value*), Integer(*start*) | Integer(*index*) | 1<<6 | 0 | -- | -- |
| memorySearch | Searches for a byte array in a byte array. | Bytes(*mem*), Bytes(*value*), Integer(*start*), Boolean(*backward*) | Integer(*index*) | 1<<6 | 0 | -- | -- |
| stringSplit | Splits a string into an array of strings. | String(*str*), String(*separator*) | String(*strings*) | 1<<8 | 0 | -- | -- |
| stringSplit | Splits a string into an array of strings. | String(*str*), String(*separator*), Boolean(*removeEmptyEntries*) | String(*strings*) | 1<<8 | 0 | -- | -- |
| strLen | Gets the length of a string. | String(*str*) | Integer(*length*) | 1<<8 | 0 | -- | -- |


## CryptoLib
|   Method  |  Description  |  Parameters  |  Return Value  |  CPU fee  |  Storage fee  |  Call Flags  |  Hardfork  |
|-----------|---------------|--------------|----------------|-----------|---------------|--------------|--------------|
| recoverSecp256K1 | Recovers the public key from a secp256k1 signature in a single byte array format. | Bytes(*messageHash*), Bytes(*signature*) | Bytes(*public key*) | 1<<15 | 0 | -- | HF_Echidna |
| ripemd160 | Computes the hash value for the specified byte array using the ripemd160 algorithm. | Bytes(*data*) | Bytes(*hash*) | 1<<15 | 0 | -- | -- |
| sha256 | Computes the hash value for the specified byte array using the sha256 algorithm. | Bytes(*data*) | Bytes(*hash*) | 1<<15 | 0 | -- | -- |
| murmur32 | Computes the hash value for the specified byte array using the murmur32 algorithm. | Bytes(*data*), Integer(*seed*) | Bytes(*hash*) | 1<<13 | 0 | -- | -- |
| keccak256 | Computes the hash value for the specified byte array using the keccak256 algorithm. | Bytes(*data*) | Bytes(*hash*) | 1<<15 | 0 | -- | HF_Cockatrice |
| verifyWithECDsa | Verifies that a digital signature is appropriate for the provided key and message using the ECDSA algorithm. | Bytes(*message*), Bytes(*pubkey*), Bytes(*signature*), NamedCurveHash(*curveHash*) | Boolean | 1<<15 | 0 | -- | -- |
| verifyWithEd25519 | Verifies that a digital signature is appropriate for the provided key and message using the Ed25519 algorithm. | Bytes(*message*), Bytes(*pubkey*), Bytes(*signature*) | Boolean | 1<<15 | 0 | -- | HF_Echidna |
| bls12381Serialize | Serialize a bls12381 point. | InteropInterface(*g*) | Bytes(*data*) | 1<<19 | 0 | -- | -- |
| bls12381Deserialize | Deserialize a bls12381 point. | Bytes(*data*) | InteropInterface(*g*) | 1<<19 | 0 | -- | -- |
| bls12381Equal | Determines whether the specified points are equal. | InteropInterface(*x*), InteropInterface(*y*) | Boolean | 1<<5 | 0 | -- | -- |
| bls12381Add | Add operation of two points. | InteropInterface(*x*), InteropInterface(*y*) | InteropInterface(*result*) | 1<<19 | 0 | -- | -- |
| bls12381Mul | Mul operation of gt point and multiplier. | InteropInterface(*x*), Bytes(*mul*), Boolean(*neg*) | InteropInterface(*result*) | 1<<21 | 0 | -- | -- |
| bls12381Pairing | Pairing operation of g1 and g2. | InteropInterface(*g1*), InteropInterface(*g2*) | InteropInterface(*result*) | 1<<23 | 0 | -- | -- |

## LedgerContract
|   Method  |  Description  |  Parameters  |  Return Value  |  CPU fee  |  Storage fee  |  Call Flags  |  Hardfork  |
|-----------|---------------|--------------|----------------|-----------|---------------|--------------|--------------|
| currentHash | Gets the hash of the current block. | -- | Hash256 | 1<<15 | 0 | ReadStates | -- |
| currentIndex | Gets the index of the current block. | -- | Integer | 1<<15 | 0 | ReadStates | -- |
| getBlock | Gets the block with the specified hash or index. | Hash256(*block hash*) or Integer(*block index*) | TrimmedBlock | 1<<15 | 0 | ReadStates | -- |
| getTransaction | Gets the transaction with the specified hash. | Hash256(*tx hash*) | Transaction | 1<<15 | 0 | ReadStates | -- |
| getTransactionSigners | Gets the signers of the transaction with the specified hash. | Hash256(*tx hash*) | Signer[] | 1<<15 | 0 | ReadStates | -- |
| getTransactionVMState | Gets the VM state of the transaction with the specified hash. | Hash256(*tx hash*) | VMState | 1<<15 | 0 | ReadStates | -- |
| getTransactionHeight | Gets the height of the transaction with the specified hash. | Hash256(*tx hash*) | Integer | 1<<15 | 0 | ReadStates | -- |
| getTransactionFromBlock | Gets the transaction with the specified hash from the block with the specified hash or index. | Hash256(*block hash*) or Integer(*block index*), Integer(*tx index*) | Transaction | 1<<16 | 0 | ReadStates | -- |

## NeoToken
|   Method  |  Description  |  Parameters  |  Return Value  |  CPU fee  |  Storage fee  |  Call Flags  |  Hardfork  |
|-----------|---------------|--------------|----------------|-----------|---------------|--------------|--------------|
| symbol | Gets the symbol of the token. | -- | String | 0 | 0 | -- | -- |
| decimals | Gets the number of decimal places of the token. | -- | Byte | 0 | 0 | -- | -- |
| totalSupply | Gets the total supply of the token. | -- | Integer | 1<<15 | 0 | ReadStates | -- |
| balanceOf | Gets the balance of the specified account. | UInt160(*account*) | Integer | 1<<15 | 0 | ReadStates | -- |
| transfer | Transfers the specified amount of token to the specified account. | UInt160(*from*), UInt160(*to*), Integer(*amount*), StackItem(*data*) | Boolean | 1<<17 | 50 | States,AllowCall,AllowNotify | -- |
| setGasPerBlock | Sets the amount of GAS generated in each block. Only committee members can call this method. | BigInteger(*gasPerBlock*) | -- | 1<<15 | 0 | States | -- |
| getGasPerBlock | Gets the amount of GAS generated in each block. | -- | BigInteger | 1<<15 | 0 | ReadStates | -- |
| setRegisterPrice | Sets the fees to be paid to register as a candidate. Only committee members can call this method. | Integer(*registerPrice*) | -- | 1<<15 | 0 | States | -- |
| getRegisterPrice | Gets the fees to be paid to register as a candidate. | -- | Integer | 1<<15 | 0 | ReadStates | -- |
| unclaimedGas | Gets the amount of unclaimed GAS in the specified account. | UInt160(*account*), Integer(*end*) | BigInteger | 1<<17 | 0 | ReadStates | -- |
| onNEP17Payment | Called when NEP-17 token is received. | UInt160(*from*), BigInteger(*amount*), StackItem(*data*) | -- | 0 | 0 | States,AllowCall,AllowNotify | HF_Echidna |
| registerCandidate | Registers a candidate. | ECPoint(*pubkey*) | Boolean | 0 | 0 | States before HF_Echidna, or States,AllowNotify after HF_Echidna | -- |
| unregisterCandidate | Unregisters a candidate. | ECPoint(*pubkey*) | Boolean | 1<<16 | 0 | States before HF_Echidna, or States,AllowNotify after HF_Echidna | -- |
| vote | Votes for a candidate. | UInt160(*account*), ECPoint(*voteTo*) | Boolean | 1<<16 | 0 | States before HF_Echidna, or States,AllowNotify after HF_Echidna | HF_Echidna |
| getCandidates | Gets the candidates. | -- | ECPoint[] | 1<<22 | 0 | ReadStates | -- |
| getAllCandidates | Gets the candidates. | -- | Iterator(/*key: ECPoint, value: CandidateState*/) | 1<<22 | 0 | ReadStates | -- |
| getCandidateVote | Gets the vote of the specified candidate. | ECPoint(*pubkey*) | BigInteger | 1<<15 | 0 | ReadStates | -- |
| getCommittee | Gets the committee. | -- | ECPoint[] | 1<<16 | 0 | ReadStates | -- |
| getAccountState | Gets the account state. | UInt160(*account*) | NeoAccountState | 1<<15 | 0 | ReadStates | -- |
| getCommitteeAddress | Gets the address of the committee. | -- | UInt160 | 1<<16 | 0 | ReadStates | HF_Cockatrice |
| getNextBlockValidators | Gets the validators of the next block. | -- | ECPoint[]/*public key*/ | 1<<16 | 0 | ReadStates | -- |

## GasToken
|   Method  |  Description  |  Parameters  |  Return Value  |  CPU fee  |  Storage fee  |  Call Flags  |  Hardfork  |
|-----------|---------------|--------------|----------------|-----------|---------------|--------------|--------------|
| symbol | Gets the symbol of the token. | -- | String | 0 | 0 | -- | -- |
| decimals | Gets the number of decimal places of the token. | -- | Byte | 0 | 0 | -- | -- |
| totalSupply | Gets the total supply of the token. | -- | Integer | 1<<15 | 0 | ReadStates | -- |
| balanceOf | Gets the balance of the specified account. | UInt160(*account*) | Integer | 1<<15 | 0 | ReadStates | -- |
| transfer | Transfers the specified amount of token to the specified account. | UInt160(*from*), UInt160(*to*), Integer(*amount*), StackItem(*data*) | Boolean | 1<<17 | 50 | States,AllowCall,AllowNotify | -- |

## PolicyContract
|   Method  |  Description  |  Parameters  |  Return Value  |  CPU fee  |  Storage fee  |  Call Flags  |  Hardfork  |
|-----------|---------------|--------------|----------------|-----------|---------------|--------------|--------------|
| getFeePerByte | Gets the network fee per transaction byte. | -- | Integer | 1<<15 | 0 | ReadStates | -- |
| setFeePerByte | Sets the network fee per transaction byte. Only committee members can call this method. | Integer(*feePerByte*) | -- | 1<<15 | 0 | States | -- |
| getExecFeeFactor | Gets the execution fee factor. | -- | Integer | 1<<15 | 0 | ReadStates | -- |
| setExecFeeFactor | Sets the execution fee factor. Only committee members can call this method. | Integer(*execFeeFactor*) | -- | 1<<15 | 0 | States | -- |
| getStoragePrice | Gets the storage price. | -- | Integer | 1<<15 | 0 | ReadStates | -- |
| setStoragePrice | Sets the storage price. Only committee members can call this method. | Integer(*storagePrice*) | -- | 1<<15 | 0 | States | -- |
| getMillisecondsPerBlock | Gets the block generation time in milliseconds. | -- | Integer | 1<<15 | 0 | ReadStates | HF_Echidna |
| setMillisecondsPerBlock | Sets the block generation time in milliseconds. Only committee members can call this method. | Integer(*millisecondsPerBlock*) | -- | 1<<15 | 0 | States,AllowNotify | HF_Echidna |
| getMaxValidUntilBlockIncrement | Gets the upper increment size of blockchain height (in blocks) exceeding. | -- | Integer | 1<<15 | 0 | ReadStates | HF_Echidna |
| setMaxValidUntilBlockIncrement | Sets the upper increment size of blockchain height (in blocks) exceeding. Only committee members can call this method. | Integer(*maxValidUntilBlockIncrement*) | -- | 1<<15 | 0 | States | HF_Echidna |
| getMaxTraceableBlocks | Gets the maximum traceable blocks. | -- | Integer | 1<<15 | 0 | ReadStates | HF_Echidna |
| setMaxTraceableBlocks | Sets the maximum traceable blocks. Only committee members can call this method. | Integer(*maxTraceableBlocks*) | -- | 1<<15 | 0 | States | HF_Echidna |
| getAttributeFee | Gets the fee for attribute. | Integer(*attribute type*) | Integer | 1<<15 | 0 | ReadStates | -- |
| setAttributeFee | Sets the fee for attribute. Only committee members can call this method. | Integer(*attribute type*), Integer(*attribute fee*) | -- | 1<<15 | 0 | States | -- |
| isBlocked | Checks if a account is blocked. | Hash160(*contract*) | Boolean | 1<<15 | 0 | ReadStates | -- |
| blockAccount | Blocks a account. Only committee members can call this method. | Hash160(*contract*) | Boolean | 1<<15 | 0 | States | -- |
| unblockAccount | Unblocks a account. Only committee members can call this method. | Hash160(*contract*) | Boolean | 1<<15 | 0 | States | -- |
| getBlockedAccounts | Gets the blocked accounts. | -- | Iterator | 1<<15 | 0 | ReadStates | HF_Faun |

## RoleManagement
|   Method  |  Description  |  Parameters  |  Return Value  |  CPU fee  |  Storage fee  |  Call Flags  |  Hardfork  |
|-----------|---------------|--------------|----------------|-----------|---------------|--------------|--------------|
| getDesignatedByRole | Gets the designated account by role. | Role(*role*), Integer(*index*) | UInt160 | 1<<15 | 0 | ReadStates | -- |
| designateAsRole | Designates the account as a role. Only committee members can call this method. | Role(*role*), ECPoint[](*public keys*) | -- | 1<<15 | 0 | States,AllowNotify | -- |

## OracleContract
|   Method  |  Description  |  Parameters  |  Return Value  |  CPU fee  |  Storage fee  |  Call Flags  |  Hardfork  |
|-----------|---------------|--------------|----------------|-----------|---------------|--------------|--------------|
| setPrice | Sets the price for an Oracle request. Only committee members can call this method. | Integer(*price*) | -- | 1<<15 | 0 | States | -- |
| getPrice | Gets the price for an Oracle request. | -- | Integer | 1<<15 | 0 | ReadStates | -- |
| finish | Finishes an Oracle response. | -- | -- | 0 | 0 | States,AllowCall,AllowNotify | -- |
| request | Creates an Oracle request. | String(*url*), String(*filter*), String(*callback*), StackItem(*userData*), Integer(*gasForResponse*) | -- | 0 | 0 | States,AllowNotify | -- |
| verify | Verifies an Oracle response. | -- | Boolean | 1<<15 | 0 | -- | -- |

## Notary
|   Method  |  Description  |  Parameters  |  Return Value  |  CPU fee  |  Storage fee  |  Call Flags  |  Hardfork  |
|-----------|---------------|--------------|----------------|-----------|---------------|--------------|--------------|
| verify | Verify checks whether the transaction is signed by one of the notaries and ensures whether deposited amount of GAS is enough to pay the actual sender's fee. | Bytes(*signature*) | Boolean | 1<<15 | 0 | ReadStates | HF_Echidna |
| onNEP17Payment | onNEP17Payment is a callback that accepts GAS transfer as Notary deposit. | UInt160(*from*), BigInteger(*amount*), StackItem(*data*) | -- | 1<<15 | 0 | States | HF_Echidna |
| lockDepositUntil | Lock deposit until the specified height. | UInt160(*account*), Integer(*till*) | Boolean | 1<<15 | 0 | States | HF_Echidna |
| expirationOf | Expiration of returns deposit lock height for specified address. | UInt160(*account*) | Integer | 1<<15 | 0 | ReadStates | HF_Echidna |
| balanceOf | Balance of returns deposited GAS amount for specified address. | UInt160(*account*) | BigInteger | 1<<15 | 0 | ReadStates | HF_Echidna |
| withdraw | Withdraw sends all deposited GAS for "from" address to "to" address. If "to" address is not specified, then "from" will be used as a sender. | UInt160(*from*), UInt160(*to*) | Boolean | 1<<15 | 0 | All | HF_Echidna |
| getMaxNotValidBeforeDelta | Returns the maximum NotValidBefore delta. | -- | Integer | 1<<15 | 0 | ReadStates | HF_Echidna |
| setMaxNotValidBeforeDelta | Sets the maximum NotValidBefore delta. Only committee members can call this method. | Integer(*value*) | -- | 1<<15 | 0 | States | HF_Echidna |