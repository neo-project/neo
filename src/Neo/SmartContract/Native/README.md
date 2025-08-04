# Native Contracts

This directory contains Neo's native smart contracts that provide core blockchain functionality.

## CryptoLib

The CryptoLib native contract provides cryptographic operations for smart contracts.

### Hash Functions
- `SHA256(byte[] data)` - SHA-256 hash
- `RIPEMD160(byte[] data)` - RIPEMD-160 hash  
- `Keccak256(byte[] data)` - Keccak-256 hash
- `Murmur32(byte[] data, uint seed)` - Murmur3 32-bit hash

### Digital Signatures
- `VerifyWithECDsa(byte[] message, byte[] pubkey, byte[] signature, NamedCurveHash curveHash)` - ECDSA verification
- `VerifyWithEd25519(byte[] message, byte[] pubkey, byte[] signature)` - Ed25519 verification
- `RecoverSecp256K1(byte[] messageHash, byte[] signature)` - Secp256k1 public key recovery

### Elliptic Curve Operations

#### BLS12-381 Support
- `Bls12381Serialize(InteropInterface g)` - Serialize BLS12-381 points
- `Bls12381Deserialize(byte[] data)` - Deserialize BLS12-381 points
- `Bls12381Equal(InteropInterface x, InteropInterface y)` - Point equality check
- `Bls12381Add(InteropInterface x, InteropInterface y)` - Point addition
- `Bls12381Mul(InteropInterface x, byte[] mul, bool neg)` - Scalar multiplication
- `Bls12381Pairing(InteropInterface g1, InteropInterface g2)` - Bilinear pairing

#### BN254 Support
- `Bn254Serialize(InteropInterface g)` - Serialize BN254 points
- `Bn254Deserialize(byte[] data)` - Deserialize BN254 points
- `Bn254Equal(InteropInterface x, InteropInterface y)` - Point equality check
- `Bn254Add(InteropInterface x, InteropInterface y)` - Point addition
- `Bn254Mul(InteropInterface x, byte[] mul, bool neg)` - Scalar multiplication
- `Bn254Pairing(InteropInterface g1, InteropInterface g2)` - Bilinear pairing
- `Bn254PairingCheck(InteropInterface[] g1, InteropInterface[] g2)` - Multi-pairing check

## Other Native Contracts

- **NeoToken** - NEO governance token
- **GasToken** - GAS utility token
- **PolicyContract** - Network policy management
- **RoleManagement** - Role-based permissions
- **OracleContract** - External data integration
- **LedgerContract** - Block and transaction queries
- **StdLib** - Standard library functions
- **ContractManagement** - Smart contract deployment and management

## Usage in Smart Contracts

Native contracts can be called from smart contracts using the `Contract.Call` method:

```csharp
// Hash data using SHA-256
var hash = (byte[])Contract.Call(CryptoLib.Hash, "sha256", data);

// Perform BN254 point addition
var result = (InteropInterface)Contract.Call(
    CryptoLib.Hash, 
    "bn254Add", 
    point1, 
    point2
);
```

For more details on specific cryptographic operations, see the [BN254 Support Documentation](../../../../docs/BN254_SUPPORT.md).