# BN254 Implementation Fix Plan

## Current Issues
1. Montgomery form handling is inconsistent
2. Field arithmetic (Fp and Scalar) has incorrect results
3. Elliptic curve points (G1/G2) are not on curve
4. Pairing operations are failing

## Root Causes
1. Mixing Montgomery and normal form representations
2. Incorrect Montgomery constants or reduction
3. Generator points may have wrong coordinates
4. Field inversion using wrong exponent

## Fix Strategy

### Phase 1: Fix Field Arithmetic
1. Ensure consistent Montgomery form usage in Fp
2. Ensure consistent Montgomery form usage in Scalar
3. Fix inversion algorithms to use correct exponents
4. Verify all arithmetic operations produce correct results

### Phase 2: Fix Elliptic Curve Operations
1. Verify G1 generator is on curve y² = x³ + 3
2. Verify G2 generator is on curve y² = x³ + 3/(i+9)
3. Fix point addition formulas if needed
4. Fix scalar multiplication

### Phase 3: Fix Pairing Operations
1. Ensure Miller loop is correct
2. Fix final exponentiation
3. Verify pairing properties e(P,Q) = e(Q,P)

## Implementation Approach
- Start with known working test vectors
- Implement step by step with verification
- Use established BN254 implementations as reference (arkworks, gnark)