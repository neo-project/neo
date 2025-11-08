# Post Quantum Cryptography Support Planning for NEO(Draft)

## Overview

Post-Quantum Cryptography (PQC), also known as Quantum-Resistant Cryptography, refers to cryptographic algorithms that can resist attacks from quantum computers. As quantum computing technology advances, traditional public-key cryptographic algorithms (such as RSA, ECDSA, Ed25519, etc.) face the risk of being broken by quantum computers.

The cryptographic algorithms currently used by the NEO blockchain include:
- **ECDSA** (secp256k1/secp256r1 curves) - Used for digital signatures
- **SHA-256/Keccak-256** - Used for hash operations

Some algorithms may not be secure against quantum computers, so migration to post-quantum cryptography needs to be planned.

## Quantum Computing Threats

### Shor's Algorithm
Shor's algorithm can break cryptographic algorithms based on integer factorization and discrete logarithm problems in polynomial time:
- **ECDSA** - Based on elliptic curve discrete logarithm problem

### Grover's Algorithm
Grover's algorithm can reduce the security of symmetric cryptography and hash functions by half:
- **SHA-256** - Security reduced from 2^256 to 2^128
- **AES-256** - Security reduced from 2^256 to 2^128

## NIST Post-Quantum Cryptography Standardization

The National Institute of Standards and Technology (NIST) began the post-quantum cryptography standardization process in 2016 and published the first round of standard algorithms in 2022.

### Standardized Algorithms

**Dilithium(ML-DSA, FIPS 204)**
- Type: Lattice-based
- Characteristics: Excellent performance, moderate signature and key sizes[1]
- Security levels: Level 1/3/5
- Recommended use: General-purpose digital signatures

```
Method              PublicKey-Size    PrivateKey-Size   SignatureSize  Security-Level
--------------------------------------------------------------------------------------
Dilithium 2         ~1300              ~2500               ~2400          1 (128-bit)
Dilithium 3         ~1950              ~4000               ~3300          3 (192-bit)
Dilithium 5         ~2600              ~4900               ~4600          5 (256-bit)
```

**FALCON(FN-DSA, FIPS 206)**
- Type: Lattice-based
- Characteristics: Small signatures, but slower key generation[2]
- Security levels: Level 1/5
- Recommended use: Scenarios requiring small signatures

```
Method             PublicKey-Size    PrivateKey-Size   SignatureSize  Security-Level
---------------------------------------------------------------------------------------
Falcon 512         ~900               ~1280                ~666            1 (128-bit)
Falcon 1024        ~1800              ~2300                ~1280           5 (256-bit)
```

**SPHINCS+(SLH-DSA,FIPS 205)**
- Type: Hash-based
- Characteristics: Security based on hash functions, but larger signatures[3]
- Security levels: Level 1/3/5
- Recommended use: Long-term secure storage

```
Method                   PublicKey-Size    PrivateKey-Size   SignatureSize  Security-Level
------------------------------------------------------------------------------------------------------
Sphincs SHA256-128f       32                 64             ~17000         1 (128-bit)
Sphincs SHA256-192f       48                 96             ~36000         3 (192-bit)
Sphincs SHA256-256f       64                128             ~50000         5 (256-bit)
```

## NEO Support Considerations

### Current Challenges

1. **Security**: Need to ensure the security of the implementation, and the algorithms are mature and stable.
2. **Compatibility**: Need to maintain compatibility with existing systems
3. **Stability**: Need to ensure the stability of the implementation, and release features smoothly.
4. **Performance**: Post-quantum algorithms are typically slower than traditional algorithms, with larger keys and signatures
5. **Standardization**: Need to wait for algorithm maturity and standardization

### Recommended Algorithm Selection

#### Asymmetric Encryption Algorithms and Digital Signatures
2. **FALCON-512** (Recommended)
   - Small signatures (~666 bytes)
   - Slower but fast enough for most use cases

1. **Dilithium 3** (Alternative)
   - Better performance than FALCON-512

#### Hash Functions
1. **SHA3-256** (Recommended)
   - Standardized and widely used

2. **BLAKE2b-256** (Alternative)
   - Better performance than SHA3-256

3. **SHA2-256** (Compatible with existing logic)
   - Widely used in current implementations

#### Libraries Recommendations
- ML-DSA has supported in `System.Security.Cryptography`[4].
- Bouncy Castle has supported PQC algorithms[5].

## Implementation Recommendations & Roadmap
For compatibility and stability, NEO should keep account system, smart contract, and other features backward compatible.
So it's better to release the new features step by step, and don't change the existing features and the way to use them(new and existing features).

### Step 1: Support SHA3 and BLAKE2b as hash functions
 - `SHA3` and `BLAKE2b` support for `CryptoLib`, then the smart contract can use them.

### Step 2: Support ML-DSA & FN-DSA
 - ML-DSA & FN-DSA support for `CryptoLib`, then the smart contract can use them.
 - PublicKey serialization/deserialization format.

### Step 3: Support PQC KeyPair for Wallets
 - PQC KeyPair support for `Wallet`, then the wallet can generate PQC KeyPair.
   - New version for account, the current version is 0x35('N'), and the `Q` is recommended to be the new version.
 - PQC Account: there are two ways to support PQC Account:
   - Keep the same as existing account system, the account is generated from the public key, and it's size is 160bit, but the RIPEMD-160 will be removed.
   - New account system, the account is generated from the public key, but it's size will be longer.

### Step 4: PQC Single-Signer Account
 - The transaction signature should be supported PQC algorithms, and the existing signature verification logic should be compatible.
   - PQC version `System.Crypto.CheckSig`

### Step 5: PQC Multi-Signer Account
 - The transaction signature should be supported PQC algorithms, and the existing signature verification logic should be compatible.
   - PQC version `System.Crypto.CheckMultisig`

## References

- [About Crystals Dilithium](https://pq-crystals.org/dilithium/index.shtml)
- [About Falcon](https://falcon-sign.info)
- [About SPHINCS+](http://sphincs.org)
- [ML-DSA in System.Security.Cryptography](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.mldsa)
- [Bouncy Castle PQC](https://github.com/bcgit/bc-csharp/tree/master/crypto/src/pqc)