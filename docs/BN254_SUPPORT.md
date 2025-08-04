# BN254 Curve Support in Neo

## Overview

Neo now supports the BN254 (alt_bn128) elliptic curve alongside the existing BLS12-381 support. BN254 is widely used in zero-knowledge proof systems and is compatible with Ethereum's precompiled contracts, making it easier to port ZK applications from Ethereum to Neo.

## Features

The BN254 implementation in Neo provides:

- **G1 Operations**: Point addition, scalar multiplication, and serialization for the base group
- **G2 Operations**: Point addition, scalar multiplication, and serialization for the extension field group
- **Pairing Operations**: Bilinear pairing computation and pairing checks
- **Native Contract Integration**: All operations are available through the CryptoLib native contract

## CryptoLib Native Contract Methods

### Bn254Serialize
```csharp
byte[] Bn254Serialize(InteropInterface point)
```
Serializes a BN254 point (G1, G2, or Gt) to its compressed byte representation.

### Bn254Deserialize
```csharp
InteropInterface Bn254Deserialize(byte[] data)
```
Deserializes a byte array to a BN254 point. The point type is determined by the data length:
- 48 bytes: G1 point
- 64 bytes: G2 point  
- 384 bytes: Gt point

### Bn254Equal
```csharp
bool Bn254Equal(InteropInterface x, InteropInterface y)
```
Checks if two BN254 points are equal. Both points must be of the same type.

### Bn254Add
```csharp
InteropInterface Bn254Add(InteropInterface x, InteropInterface y)
```
Adds two BN254 points. Supports addition of points in the same group (G1, G2, or Gt).

### Bn254Mul
```csharp
InteropInterface Bn254Mul(InteropInterface point, byte[] scalar, bool neg)
```
Performs scalar multiplication on a BN254 point. The scalar should be 32 bytes in little-endian format. If `neg` is true, the scalar is negated before multiplication.

### Bn254Pairing
```csharp
InteropInterface Bn254Pairing(InteropInterface g1, InteropInterface g2)
```
Computes the bilinear pairing e(g1, g2) where g1 is a G1 point and g2 is a G2 point. Returns a Gt element.

### Bn254PairingCheck
```csharp
bool Bn254PairingCheck(InteropInterface[] g1, InteropInterface[] g2)
```
Performs a multi-pairing check: verifies if e(g1[0], g2[0]) * ... * e(g1[n], g2[n]) = 1. This is commonly used in zero-knowledge proof verification.

## Usage Examples

### Basic Point Operations
```csharp
// Serialize and deserialize a G1 point
var g1 = G1Affine.Generator;
var serialized = CryptoLib.Bn254Serialize(new InteropInterface(g1));
var deserialized = CryptoLib.Bn254Deserialize(serialized);

// Add two G1 points
var sum = CryptoLib.Bn254Add(
    new InteropInterface(G1Affine.Generator),
    new InteropInterface(G1Affine.Generator)
);

// Scalar multiplication
var scalar = new byte[32];
scalar[0] = 2; // Multiply by 2
var doubled = CryptoLib.Bn254Mul(
    new InteropInterface(G1Affine.Generator),
    scalar,
    false
);
```

### Pairing Operations
```csharp
// Compute a pairing
var pairing = CryptoLib.Bn254Pairing(
    new InteropInterface(G1Affine.Generator),
    new InteropInterface(G2Affine.Generator)
);

// Multi-pairing check for ZK proof verification
var g1Points = new[] { 
    new InteropInterface(g1_1),
    new InteropInterface(g1_2)
};
var g2Points = new[] {
    new InteropInterface(g2_1),
    new InteropInterface(g2_2)
};
bool isValid = CryptoLib.Bn254PairingCheck(g1Points, g2Points);
```

## Technical Details

### Curve Parameters
- **Field Prime (p)**: 21888242871839275222246405745257275088696311157297823662689037894645226208583
- **Scalar Field (r)**: 21888242871839275222246405745257275088548364400416034343698204186575808495617
- **Curve Equation**: y² = x³ + 3

### Point Encoding
- **G1 Points**: 48 bytes compressed (x-coordinate + flags)
- **G2 Points**: 64 bytes compressed (x-coordinate in Fp2 + flags)
- **Gt Elements**: 384 bytes (full Fp12 element)

### Compression Flags
Points use the following flag bits in the first byte:
- Bit 7 (0x80): Compression flag (always set for compressed points)
- Bit 6 (0x40): Infinity flag (set if point is identity/infinity)
- Bit 5 (0x20): Sort flag (indicates y-coordinate sign)

## Performance Considerations

- Point addition: O(1) field operations
- Scalar multiplication: O(log n) point additions
- Pairing computation: Most expensive operation, use sparingly
- Multi-pairing check: More efficient than individual pairings

## Compatibility

The BN254 implementation is compatible with:
- Ethereum's alt_bn128 precompiled contracts
- Popular ZK libraries (libsnark, arkworks, etc.)
- Existing BN254-based zero-knowledge proof systems

## Security Notes

1. BN254 provides approximately 100 bits of security, which is lower than BLS12-381's 128 bits
2. The curve is still considered secure for most applications
3. Always validate points are on the curve before operations
4. Use proper randomness for scalar generation

## Migration from BLS12-381

If migrating from BLS12-381 to BN254:
1. Replace `Bls12381*` method calls with `Bn254*` equivalents
2. Update point sizes in serialization (G1: 48→48, G2: 96→64, Gt: 576→384)
3. Adjust gas costs in smart contracts (BN254 operations are generally faster)
4. Verify security requirements are still met with lower security level