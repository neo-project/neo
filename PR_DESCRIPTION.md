# Description

This PR adds comprehensive BN254 (alt_bn128) elliptic curve support to Neo's CryptoLib native contract. BN254 is a pairing-friendly curve widely used in zero-knowledge proof systems and is compatible with Ethereum's precompiled contracts, enabling cross-chain ZK applications.

The implementation provides complete cryptographic operations including field arithmetic, group operations, and bilinear pairings, following the same architectural patterns as the existing BLS12-381 support. This enables developers to build zkSNARKs, privacy-preserving applications, and cross-chain verification systems on Neo.

**Key Features:**
- Full BN254 elliptic curve implementation with Fp, Fp2, and scalar field arithmetic
- G1, G2 group operations in both affine and projective coordinates  
- Bilinear pairing computations for zero-knowledge proof verification
- Point compression/decompression with standards compliance
- Native contract integration with 7 new CryptoLib methods
- Complete compatibility with Ethereum's alt_bn128 precompiled contracts

Fixes # (issue)

## Type of change

- [x] New feature (non-breaking change which adds functionality)
- [x] This change requires a documentation update

# How Has This Been Tested?

- [x] Unit Testing - Comprehensive test suite covering all BN254 operations
- [x] Integration Testing - Native contract method validation with edge cases
- [x] Compilation Testing - Multi-target framework compatibility verification
- [x] Serialization Testing - Point compression/decompression correctness

**Test Configuration**:
- **Field Arithmetic Tests**: Fp, Fp2, Scalar operations with edge cases and Montgomery form validation
- **Group Operation Tests**: Point addition, doubling, scalar multiplication for G1/G2 groups
- **Pairing Tests**: Bilinear pairing computation and multi-pairing verification
- **Native Contract Tests**: All 7 CryptoLib methods with parameter validation and error handling
- **Cross-Platform Tests**: netstandard2.1 and net9.0 target framework compatibility

# Checklist:

- [x] My code follows the style guidelines of this project
- [x] I have performed a self-review of my code
- [x] I have commented my code, particularly in hard-to-understand areas
- [x] I have made corresponding changes to the documentation
- [x] My changes generate no new warnings
- [x] I have added tests that prove my fix is effective or that my feature works
- [x] New and existing unit tests pass locally with my changes
- [x] Any dependent changes have been merged and published in downstream modules