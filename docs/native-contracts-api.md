# Native Contracts API
Native contracts are the contracts that are implemented in the Neo blockchain,
and native contract APIsare the methods that are provided by the native contracts.

When calling a native contract method by transaction script, there are several tips and notes:
1. A part of native contract methods require CallFlags. If no such CallFlags is provided, the call will be failed. 
2. Some native contract methods are only allowed to be called before or after a certain hardfork.
3. A native contract method may have different behaviors in different hardforks.

## Table of Contents

1. [ContractManagement](#contractmanagement)
2. [StdLib](#stdlib)
3. [CryptoLib](#cryptolib)
4. [LedgerContract](#ledgercontract)
5. [NeoToken](#neotoken)
6. [GasToken](#gastoken)
7. [PolicyContract](#policycontract)
8. [RoleManagement](#rolemanagement)
9. [OracleContract](#oraclecontract)
10. [Notary](#notary)
11. [Treasury](#treasury)

## ContractManagement

| Method | Summary | Parameters | Return Value | CPU fee | Storage fee | Call Flags | Hardfork |
|--------|---------|------------|--------------|---------|-------------|------------|----------|
| getMinimumDeploymentFee | Gets the minimum deployment fee for deploying a contract. | -- | Int64 | 1<<15 | 0 | ReadStates | -- |
| setMinimumDeploymentFee | Sets the minimum deployment fee for deploying a contract. Only committee members can call this method. | BigInteger(*value*) | Void | 1<<15 | 0 | States | -- |
| getContract | Gets the deployed contract with the specified hash. | UInt160(*hash*) | ContractState | 1<<15 | 0 | ReadStates | -- |
| isContract | Check if exists the deployed contract with the specified hash. | UInt160(*hash*) | Boolean | 1<<14 | 0 | ReadStates | HF_Echidna |
| getContractById | Maps specified ID to deployed contract. | Int32(*id*) | ContractState | 1<<15 | 0 | ReadStates | -- |
| getContractHashes | Gets hashes of all non native deployed contracts. | -- | IIterator | 1<<15 | 0 | ReadStates | -- |
| hasMethod | Check if a method exists in a contract. | UInt160(*hash*), String(*method*), Int32(*pcount*) | Boolean | 1<<15 | 0 | ReadStates | -- |
| deploy | Deploys a contract. It needs to pay the deployment fee and storage fee. | Byte[](*nefFile*), Byte[](*manifest*) | ContractState | 0 | 0 | States,AllowNotify | -- |
| deploy | Deploys a contract. It needs to pay the deployment fee and storage fee. | Byte[](*nefFile*), Byte[](*manifest*), StackItem(*data*) | ContractState | 0 | 0 | States,AllowNotify | -- |
| update | Updates a contract. It needs to pay the storage fee. | Byte[](*nefFile*), Byte[](*manifest*) | Void | 0 | 0 | States,AllowNotify | -- |
| update | Updates a contract. It needs to pay the storage fee. | Byte[](*nefFile*), Byte[](*manifest*), StackItem(*data*) | Void | 0 | 0 | States,AllowNotify | -- |
| destroy | Destroys a contract. | -- | Void | 1<<15 | 0 | States,AllowNotify | -- |


## StdLib

| Method | Summary | Parameters | Return Value | CPU fee | Storage fee | Call Flags | Hardfork |
|--------|---------|------------|--------------|---------|-------------|------------|----------|
| serialize | -- | StackItem(*item*) | Byte[] | 1<<12 | 0 | -- | -- |
| deserialize | -- | Byte[](*data*) | StackItem | 1<<14 | 0 | -- | -- |
| jsonSerialize | -- | StackItem(*item*) | Byte[] | 1<<12 | 0 | -- | -- |
| jsonDeserialize | -- | Byte[](*json*) | StackItem | 1<<14 | 0 | -- | -- |
| itoa | Converts an integer to a String. | BigInteger(*value*) | String | 1<<12 | 0 | -- | -- |
| itoa | Converts an integer to a String. | BigInteger(*value*), Int32(*base*) | String | 1<<12 | 0 | -- | -- |
| atoi | Converts a String to an integer. | String(*value*) | BigInteger | 1<<6 | 0 | -- | -- |
| atoi | Converts a String to an integer. | String(*value*), Int32(*base*) | BigInteger | 1<<6 | 0 | -- | -- |
| base64Encode | Encodes a byte array into a base64 String. | Byte[](*data*) | String | 1<<5 | 0 | -- | -- |
| base64Decode | Decodes a byte array from a base64 String. | String(*s*) | Byte[] | 1<<5 | 0 | -- | -- |
| base64UrlEncode | Encodes a byte array into a base64Url string. | String(*data*) | String | 1<<5 | 0 | -- | HF_Echidna |
| base64UrlDecode | Decodes a byte array from a base64Url string. | String(*s*) | String | 1<<5 | 0 | -- | HF_Echidna |
| base58Encode | Encodes a byte array into a base58 String. | Byte[](*data*) | String | 1<<13 | 0 | -- | -- |
| base58Decode | Decodes a byte array from a base58 String. | String(*s*) | Byte[] | 1<<10 | 0 | -- | -- |
| base58CheckEncode | Converts a byte array to its equivalent String representation that is encoded with base-58 digits. The encoded String contains the checksum of the binary data. | Byte[](*data*) | String | 1<<16 | 0 | -- | -- |
| base58CheckDecode | Converts the specified String, which encodes binary data as base-58 digits, to an equivalent byte array. The encoded String contains the checksum of the binary data. | String(*s*) | Byte[] | 1<<16 | 0 | -- | -- |
| hexEncode | -- | Byte[](*bytes*) | String | 1<<5 | 0 | -- | HF_Faun |
| hexDecode | -- | String(*str*) | Byte[] | 1<<5 | 0 | -- | HF_Faun |
| memoryCompare | -- | Byte[](*str1*), Byte[](*str2*) | Int32 | 1<<5 | 0 | -- | -- |
| memorySearch | -- | Byte[](*mem*), Byte[](*value*) | Int32 | 1<<6 | 0 | -- | -- |
| memorySearch | -- | Byte[](*mem*), Byte[](*value*), Int32(*start*) | Int32 | 1<<6 | 0 | -- | -- |
| memorySearch | -- | Byte[](*mem*), Byte[](*value*), Int32(*start*), Boolean(*backward*) | Int32 | 1<<6 | 0 | -- | -- |
| stringSplit | -- | String(*str*), String(*separator*) | String[] | 1<<8 | 0 | -- | -- |
| stringSplit | -- | String(*str*), String(*separator*), Boolean(*removeEmptyEntries*) | String[] | 1<<8 | 0 | -- | -- |
| strLen | -- | String(*str*) | Int32 | 1<<8 | 0 | -- | -- |


## CryptoLib

| Method | Summary | Parameters | Return Value | CPU fee | Storage fee | Call Flags | Hardfork |
|--------|---------|------------|--------------|---------|-------------|------------|----------|
| bls12381Serialize | Serialize a bls12381 point. | InteropInterface(*g*) | Byte[] | 1<<19 | 0 | -- | -- |
| bls12381Deserialize | Deserialize a bls12381 point. | Byte[](*data*) | InteropInterface | 1<<19 | 0 | -- | -- |
| bls12381Equal | Determines whether the specified points are equal. | InteropInterface(*x*), InteropInterface(*y*) | Boolean | 1<<5 | 0 | -- | -- |
| bls12381Add | Add operation of two points. | InteropInterface(*x*), InteropInterface(*y*) | InteropInterface | 1<<19 | 0 | -- | -- |
| bls12381Mul | Mul operation of gt point and multiplier | InteropInterface(*x*), Byte[](*mul*), Boolean(*neg*) | InteropInterface | 1<<21 | 0 | -- | -- |
| bls12381Pairing | Pairing operation of g1 and g2 | InteropInterface(*g1*), InteropInterface(*g2*) | InteropInterface | 1<<23 | 0 | -- | -- |
| recoverSecp256K1 | Recovers the public key from a secp256k1 signature in a single byte array format. | Byte[](*messageHash*), Byte[](*signature*) | Byte[] | 1<<15 | 0 | -- | HF_Echidna |
| ripemd160 | Computes the hash value for the specified byte array using the ripemd160 algorithm. | Byte[](*data*) | Byte[] | 1<<15 | 0 | -- | -- |
| sha256 | Computes the hash value for the specified byte array using the sha256 algorithm. | Byte[](*data*) | Byte[] | 1<<15 | 0 | -- | -- |
| murmur32 | Computes the hash value for the specified byte array using the murmur32 algorithm. | Byte[](*data*), UInt32(*seed*) | Byte[] | 1<<13 | 0 | -- | -- |
| keccak256 | Computes the hash value for the specified byte array using the keccak256 algorithm. | Byte[](*data*) | Byte[] | 1<<15 | 0 | -- | HF_Cockatrice |
| verifyWithECDsa | Verifies that a digital signature is appropriate for the provided key and message using the ECDSA algorithm. | Byte[](*message*), Byte[](*pubkey*), Byte[](*signature*), NamedCurveHash(*curveHash*) | Boolean | 1<<15 | 0 | -- | HF_Cockatrice |
| verifyWithECDsa | -- | Byte[](*message*), Byte[](*pubkey*), Byte[](*signature*), NamedCurveHash(*curve*) | Boolean | 1<<15 | 0 | -- | Deprecated in HF_Cockatrice |
| verifyWithEd25519 | Verifies that a digital signature is appropriate for the provided key and message using the Ed25519 algorithm. | Byte[](*message*), Byte[](*pubkey*), Byte[](*signature*) | Boolean | 1<<15 | 0 | -- | HF_Echidna |


## LedgerContract

| Method | Summary | Parameters | Return Value | CPU fee | Storage fee | Call Flags | Hardfork |
|--------|---------|------------|--------------|---------|-------------|------------|----------|
| currentHash | Gets the hash of the current block. | -- | UInt256 | 1<<15 | 0 | ReadStates | -- |
| currentIndex | Gets the index of the current block. | -- | UInt32 | 1<<15 | 0 | ReadStates | -- |
| getBlock | -- | Byte[](*indexOrHash*) | TrimmedBlock | 1<<15 | 0 | ReadStates | -- |
| getTransaction | -- | UInt256(*hash*) | Transaction | 1<<15 | 0 | ReadStates | -- |
| getTransactionSigners | -- | UInt256(*hash*) | Signer[] | 1<<15 | 0 | ReadStates | -- |
| getTransactionVMState | -- | UInt256(*hash*) | VMState | 1<<15 | 0 | ReadStates | -- |
| getTransactionHeight | -- | UInt256(*hash*) | Int32 | 1<<15 | 0 | ReadStates | -- |
| getTransactionFromBlock | -- | Byte[](*blockIndexOrHash*), Int32(*txIndex*) | Transaction | 1<<16 | 0 | ReadStates | -- |


## NeoToken

| Method | Summary | Parameters | Return Value | CPU fee | Storage fee | Call Flags | Hardfork |
|--------|---------|------------|--------------|---------|-------------|------------|----------|
| totalSupply | -- | -- | BigInteger | 1<<15 | 0 | ReadStates | -- |
| setGasPerBlock | Sets the amount of GAS generated in each block. Only committee members can call this method. | BigInteger(*gasPerBlock*) | Void | 1<<15 | 0 | States | -- |
| getGasPerBlock | Gets the amount of GAS generated in each block. | -- | BigInteger | 1<<15 | 0 | ReadStates | -- |
| setRegisterPrice | Sets the fees to be paid to register as a candidate. Only committee members can call this method. | Int64(*registerPrice*) | Void | 1<<15 | 0 | States | -- |
| getRegisterPrice | Gets the fees to be paid to register as a candidate. | -- | Int64 | 1<<15 | 0 | ReadStates | -- |
| unclaimedGas | Get the amount of unclaimed GAS in the specified account. | UInt160(*account*), UInt32(*end*) | BigInteger | 1<<17 | 0 | ReadStates | -- |
| onNEP17Payment | Handles the payment of GAS. | UInt160(*from*), BigInteger(*amount*), StackItem(*data*) | Void | 0 | 0 | States,AllowNotify | HF_Echidna |
| registerCandidate | Registers a candidate. | ECPoint(*pubkey*) | Boolean | 0 | 0 | States | Deprecated in HF_Echidna |
| registerCandidate | Registers a candidate. | ECPoint(*pubkey*) | Boolean | 0 | 0 | States,AllowNotify | HF_Echidna |
| unregisterCandidate | Unregisters a candidate. | ECPoint(*pubkey*) | Boolean | 1<<16 | 0 | States | Deprecated in HF_Echidna |
| unregisterCandidate | Unregisters a candidate. | ECPoint(*pubkey*) | Boolean | 1<<16 | 0 | States,AllowNotify | HF_Echidna |
| vote | Votes for a candidate. | UInt160(*account*), ECPoint(*voteTo*) | Boolean | 1<<16 | 0 | States | Deprecated in HF_Echidna |
| vote | Votes for a candidate. | UInt160(*account*), ECPoint(*voteTo*) | Boolean | 1<<16 | 0 | States,AllowNotify | HF_Echidna |
| getCandidates | Gets the first 256 registered candidates. | -- | ValueTuple`2[] | 1<<22 | 0 | ReadStates | -- |
| getAllCandidates | Gets the registered candidates iterator. | -- | IIterator | 1<<22 | 0 | ReadStates | -- |
| getCandidateVote | Gets votes from specific candidate. | ECPoint(*pubKey*) | BigInteger | 1<<15 | 0 | ReadStates | -- |
| getCommittee | Gets all the members of the committee. | -- | ECPoint[] | 1<<16 | 0 | ReadStates | -- |
| getAccountState | Get account state. | UInt160(*account*) | NeoAccountState | 1<<15 | 0 | ReadStates | -- |
| getCommitteeAddress | Gets the address of the committee. | -- | UInt160 | 1<<16 | 0 | ReadStates | HF_Cockatrice |
| getNextBlockValidators | Gets the validators of the next block. | -- | ECPoint[] | 1<<16 | 0 | ReadStates | -- |
| balanceOf | Gets the balance of the specified account. | UInt160(*account*) | BigInteger | 1<<15 | 0 | ReadStates | -- |
| transfer | -- | UInt160(*from*), UInt160(*to*), BigInteger(*amount*), StackItem(*data*) | Boolean | 1<<17 | 50 | All | -- |
| symbol | -- | -- | String | 0 | 0 | -- | -- |
| decimals | -- | -- | Byte | 0 | 0 | -- | -- |


## GasToken

| Method | Summary | Parameters | Return Value | CPU fee | Storage fee | Call Flags | Hardfork |
|--------|---------|------------|--------------|---------|-------------|------------|----------|
| totalSupply | Gets the total supply of the token. | -- | BigInteger | 1<<15 | 0 | ReadStates | -- |
| balanceOf | Gets the balance of the specified account. | UInt160(*account*) | BigInteger | 1<<15 | 0 | ReadStates | -- |
| transfer | -- | UInt160(*from*), UInt160(*to*), BigInteger(*amount*), StackItem(*data*) | Boolean | 1<<17 | 50 | All | -- |
| symbol | -- | -- | String | 0 | 0 | -- | -- |
| decimals | -- | -- | Byte | 0 | 0 | -- | -- |


## PolicyContract

| Method | Summary | Parameters | Return Value | CPU fee | Storage fee | Call Flags | Hardfork |
|--------|---------|------------|--------------|---------|-------------|------------|----------|
| getFeePerByte | Gets the network fee per transaction byte. | -- | Int64 | 1<<15 | 0 | ReadStates | -- |
| getExecFeeFactor | Gets the execution fee factor. This is a multiplier that can be adjusted by the committee to adjust the system fees for transactions. | -- | UInt32 | 1<<15 | 0 | ReadStates | -- |
| getStoragePrice | Gets the storage price. | -- | UInt32 | 1<<15 | 0 | ReadStates | -- |
| getMillisecondsPerBlock | Gets the block generation time in milliseconds. | -- | UInt32 | 1<<15 | 0 | ReadStates | HF_Echidna |
| getMaxValidUntilBlockIncrement | Gets the upper increment size of blockchain height (in blocks) exceeding that a transaction should fail validation. | -- | UInt32 | 1<<15 | 0 | ReadStates | HF_Echidna |
| getMaxTraceableBlocks | Gets the length of the chain accessible to smart contracts. | -- | UInt32 | 1<<15 | 0 | ReadStates | HF_Echidna |
| getAttributeFee | Gets the fee for attribute before Echidna hardfork. NotaryAssisted attribute type not supported. | Byte(*attributeType*) | UInt32 | 1<<15 | 0 | ReadStates | Deprecated in HF_Echidna |
| getAttributeFee | Gets the fee for attribute after Echidna hardfork. NotaryAssisted attribute type supported. | Byte(*attributeType*) | UInt32 | 1<<15 | 0 | ReadStates | HF_Echidna |
| isBlocked | Determines whether the specified account is blocked. | UInt160(*account*) | Boolean | 1<<15 | 0 | ReadStates | -- |
| setMillisecondsPerBlock | Sets the block generation time in milliseconds. | UInt32(*value*) | Void | 1<<15 | 0 | States,AllowNotify | HF_Echidna |
| setAttributeFee | Sets the fee for attribute before Echidna hardfork. NotaryAssisted attribute type not supported. | Byte(*attributeType*), UInt32(*value*) | Void | 1<<15 | 0 | States | Deprecated in HF_Echidna |
| setAttributeFee | Sets the fee for attribute after Echidna hardfork. NotaryAssisted attribute type supported. | Byte(*attributeType*), UInt32(*value*) | Void | 1<<15 | 0 | States | HF_Echidna |
| setFeePerByte | -- | Int64(*value*) | Void | 1<<15 | 0 | States | -- |
| setExecFeeFactor | -- | UInt32(*value*) | Void | 1<<15 | 0 | States | -- |
| setStoragePrice | -- | UInt32(*value*) | Void | 1<<15 | 0 | States | -- |
| setMaxValidUntilBlockIncrement | -- | UInt32(*value*) | Void | 1<<15 | 0 | States | HF_Echidna |
| setMaxTraceableBlocks | Sets the length of the chain accessible to smart contracts. | UInt32(*value*) | Void | 1<<15 | 0 | States | HF_Echidna |
| blockAccount | -- | UInt160(*account*) | Boolean | 1<<15 | 0 | States | -- |
| unblockAccount | -- | UInt160(*account*) | Boolean | 1<<15 | 0 | States | -- |
| getBlockedAccounts | -- | -- | StorageIterator | 1<<15 | 0 | ReadStates | HF_Faun |


## RoleManagement

| Method | Summary | Parameters | Return Value | CPU fee | Storage fee | Call Flags | Hardfork |
|--------|---------|------------|--------------|---------|-------------|------------|----------|
| getDesignatedByRole | Gets the list of nodes for the specified role. | Role(*role*), UInt32(*index*) | ECPoint[] | 1<<15 | 0 | ReadStates | -- |
| designateAsRole | -- | Role(*role*), ECPoint[](*nodes*) | Void | 1<<15 | 0 | States,AllowNotify | -- |


## OracleContract

| Method | Summary | Parameters | Return Value | CPU fee | Storage fee | Call Flags | Hardfork |
|--------|---------|------------|--------------|---------|-------------|------------|----------|
| setPrice | Sets the price for an Oracle request. Only committee members can call this method. | Int64(*price*) | Void | 1<<15 | 0 | States | -- |
| getPrice | Gets the price for an Oracle request. | -- | Int64 | 1<<15 | 0 | ReadStates | -- |
| finish | Finishes an Oracle response. | -- | Void | 0 | 0 | All | -- |
| request | -- | String(*url*), String(*filter*), String(*callback*), StackItem(*userData*), Int64(*gasForResponse*) | Void | 0 | 0 | States,AllowNotify | -- |
| verify | -- | -- | Boolean | 1<<15 | 0 | -- | -- |


## Notary

| Method | Summary | Parameters | Return Value | CPU fee | Storage fee | Call Flags | Hardfork |
|--------|---------|------------|--------------|---------|-------------|------------|----------|
| verify | Verify checks whether the transaction is signed by one of the notaries and ensures whether deposited amount of GAS is enough to pay the actual sender's fee. | Byte[](*signature*) | Boolean | 1<<15 | 0 | ReadStates | -- |
| onNEP17Payment | OnNEP17Payment is a callback that accepts GAS transfer as Notary deposit. It also sets the deposit's lock height after which deposit can be withdrawn. | UInt160(*from*), BigInteger(*amount*), StackItem(*data*) | Void | 1<<15 | 0 | States | -- |
| lockDepositUntil | Lock asset until the specified height is unlocked. | UInt160(*account*), UInt32(*till*) | Boolean | 1<<15 | 0 | States | -- |
| expirationOf | ExpirationOf returns deposit lock height for specified address. | UInt160(*account*) | UInt32 | 1<<15 | 0 | ReadStates | -- |
| balanceOf | BalanceOf returns deposited GAS amount for specified address. | UInt160(*account*) | BigInteger | 1<<15 | 0 | ReadStates | -- |
| withdraw | Withdraw sends all deposited GAS for "from" address to "to" address. If "to" address is not specified, then "from" will be used as a sender. | UInt160(*from*), UInt160(*to*) | Boolean | 1<<15 | 0 | All | -- |
| getMaxNotValidBeforeDelta | GetMaxNotValidBeforeDelta is Notary contract method and returns the maximum NotValidBefore delta. | -- | UInt32 | 1<<15 | 0 | ReadStates | -- |
| setMaxNotValidBeforeDelta | SetMaxNotValidBeforeDelta is Notary contract method and sets the maximum NotValidBefore delta. | UInt32(*value*) | Void | 1<<15 | 0 | States | -- |


## Treasury

| Method | Summary | Parameters | Return Value | CPU fee | Storage fee | Call Flags | Hardfork |
|--------|---------|------------|--------------|---------|-------------|------------|----------|
| verify | Verify checks whether the transaction is signed by the committee. | -- | Boolean | 1<<15 | 0 | ReadStates | -- |
| onNEP17Payment | OnNEP17Payment callback. | UInt160(*from*), BigInteger(*amount*), StackItem(*data*) | Void | 1<<15 | 0 | States | -- |
| onNEP11Payment | OnNEP11Payment callback. | UInt160(*from*), BigInteger(*amount*), Byte[](*tokenId*), StackItem(*data*) | Void | 1<<15 | 0 | States | -- |


