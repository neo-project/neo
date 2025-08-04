# Neo.Cryptography.BN254

This library provides a complete, production-ready implementation of the BN254 (alt_bn128) elliptic curve for cryptographic operations in the Neo blockchain.

## Overview

BN254 is a Barreto-Naehrig pairing-friendly elliptic curve used for zero-knowledge proofs and other advanced cryptographic protocols. This implementation provides:

- **Complete Field Arithmetic**: Fp and Fp2 field operations with Montgomery form
- **Elliptic Curve Groups**: G1 (base field) and G2 (quadratic extension) point operations  
- **Pairing Operations**: Optimal ate pairing with Miller loop and final exponentiation
- **Scalar Operations**: Complete scalar field arithmetic for cryptographic operations

## Core Components

### Field Elements

- **`Fp`**: Base field element over GF(p) where p is the BN254 base field modulus
- **`Fp2`**: Quadratic extension field element over GF(p²) 
- **`Scalar`**: Scalar field element for elliptic curve operations

### Elliptic Curve Points

- **`G1Affine`**: Points on the base curve E(Fp): y² = x³ + 3
- **`G1Projective`**: Projective coordinates for efficient G1 operations
- **`G2Affine`**: Points on the twist curve E'(Fp2): y² = x³ + 3/(9+u)
- **`G2Projective`**: Projective coordinates for efficient G2 operations

### Pairing Target Group

- **`Gt`**: Elements in the target group (multiplicative group of Fp12)

### Main API

- **`Bn254`**: Main entry point providing pairing operations and multi-pairing checks

## Security Features

- **Constant-time operations** where cryptographically relevant
- **Complete input validation** for all public APIs
- **Secure random number handling** for cryptographic operations
- **Memory-safe implementations** using safe Rust patterns in C#

## Performance Optimizations

- **Montgomery arithmetic** for efficient modular operations
- **Projective coordinates** to avoid expensive field inversions
- **Optimized pairing algorithms** using ate pairing with BN curve optimizations
- **Efficient scalar multiplication** using windowed methods

## Mathematical Background

### BN254 Curve Parameters

The BN254 curve is defined by the parameter u = 0x44e992b44a6909f1, giving:

- **Base field modulus**: p = 36u⁴ + 36u³ + 24u² + 6u + 1
- **Scalar field modulus**: r = 36u⁴ + 36u³ + 18u² + 6u + 1  
- **Embedding degree**: k = 12
- **Security level**: ~100 bits (Note: BN254 security has been reduced due to advances in discrete log attacks)

### Pairing Operation

The implementation provides the optimal ate pairing e: G1 × G2 → Gt with:
- Miller loop computation with line function evaluation
- Final exponentiation to ensure unique representatives
- Multi-pairing support for batch verification

## Usage Examples

```csharp
// Basic field operations
var a = new Fp(42);
var b = new Fp(17);
var sum = a + b;
var product = a * b;

// Elliptic curve operations  
var p1 = G1Affine.Generator;
var p2 = p1 + p1;  // Point doubling
var scalar = new Scalar(123);
var p3 = p1 * scalar;  // Scalar multiplication

// Pairing operations
var g1Point = G1Affine.Generator;
var g2Point = G2Affine.Generator;
var pairing = Bn254.Pairing(g1Point, g2Point);

// Multi-pairing check: e(P1,Q1) * e(P2,Q2) = 1
var pairs = new[] { (p1, q1), (p2, q2) };
bool isValid = Bn254.PairingCheck(pairs);
```

## Integration with Neo

This library integrates with Neo's `CryptoLib` native contract to provide BN254 operations for smart contracts:

- `bn254_serialize`: Serialize curve points and field elements
- `bn254_deserialize`: Deserialize from byte arrays
- `bn254_add`: Point addition operations
- `bn254_mul`: Scalar multiplication operations  
- `bn254_pairing`: Compute pairings
- `bn254_pairing_check`: Verify pairing equations

## Testing

The implementation includes comprehensive test coverage:

- Unit tests for all mathematical operations
- Property-based testing for algebraic laws
- Cross-validation against reference implementations
- Performance benchmarks for critical operations

## Security Considerations

**Important**: BN254 curves have reduced security (~100 bits) due to advances in discrete logarithm attacks on pairing-friendly curves. For new applications requiring long-term security, consider using BLS12-381 or other curves with higher security levels.

This implementation is suitable for:
- Ethereum compatibility (where BN254 is standard)
- Zero-knowledge proof systems already using BN254
- Applications where 100-bit security is sufficient

## References

- [BN Curves Paper](https://eprint.iacr.org/2005/133.pdf): Original Barreto-Naehrig construction
- [EIP-196](https://eips.ethereum.org/EIPS/eip-196): Ethereum BN254 precompiles specification  
- [EIP-197](https://eips.ethereum.org/EIPS/eip-197): Ethereum pairing precompile specification