# Native composite verification receipts (unreleased ABI 2)

A pinned composite verifier is trusted to implement its declared threshold policy, just as an ordinary pinned verifier is trusted to return an honest Boolean. A manifest name or profile marker is not a certificate of module behavior. Core enforces receipt shape, active code identity and callback authority; it cannot infer a cryptographic threshold proof from a list of child identities.

Composite verifiers implement non-safe `validateCompositeSignature(Hash160, Array) -> Array` and `postExecuteComposite(Hash160, Array, Any, Array) -> Void`. Validation returns the exact array `[true, approvedChildren, policyCommitment]`: exact Boolean true; an exact nonempty Array of at most two distinct, immutable 20-byte ByteStrings in active-roster order; an immutable 32-byte ByteString commitment. Structs, Buffers, coercible values, foreign children and reordered children are rejected. The native profile permits three configured children, threshold at most two and three aggregate distinct signer domains. Configuration rechecks current signer domains atomically, including changes to active children.

The receipt is copied into bounded host-owned per-operation memory. Verification checks then discards it. Application always validates afresh. Each batch item owns a separate receipt. The target and hook never receive it; no external API can inject one. The root receives a fresh copy only during its matching post callback. Its post callback receives authority for exactly the approved direct children, with all active module code bindings checked again after the target and post callback. Existing account locks, lexical callback grants and fault reset govern reentry and cleanup. Any post failure rolls back the target, nonce, module writes and notifications together.

MultiSig selects the first threshold-valid children in configured order, with later supplied signatures granting no post authority. It binds threshold, ordered children and each child's freshly queried signer domains in the receipt commitment, checking that policy again in post without repeating cryptographic signature verification. This is not a persistent authorization cache. Dynamic third-party signer domains cannot be assumed stable; the commitment must match the fresh policy. The root verifier budget remains 1 GAS, hook budget 2.5 GAS and Verification transaction ceiling 1.5 GAS.

Every module must explicitly declare `extra.smartAccount.profileDigest` equal to the service parameter digest and exact Boolean `compositeVerifier`. Only verifier roots can set the latter true. Verifier discovery must agree with it and the exact callback ABI. Hooks declare false, independently of hook composition. Old ABI-2 artifacts without this profile commitment are rejected. Identity version 1, operation six-field encoding, four-argument execution and authority/configuration counters are unchanged.

The core regression suite exercises real NeoVM callbacks, including exact receipt types, an old scalar callback that aborts, Verification/Application separation, different approvals in two batch items, receipt mutation, root and unselected-child update/destruction, post-callback destruction and active-child domain expansion. A faulted ApplicationEngine can retain its uncommitted temporary cache for diagnostics; normal ledger processing discards it. Tests assert the parent snapshot and notifications remain unchanged rather than claiming the discarded cache was erased. Persisted private-chain tests are the separate integration gate.

A scalar verifier cannot publish a dependency roster or obtain child callback grants merely by using the MultiSig manifest name. Dependency publication requires the pinned composite capability, and execution independently rejects a scalar root with active children. This prevents selecting the old Boolean callback path while acquiring composite child authority.

`canonicalP256PublicKey(publicKey: ByteArray) -> ByteArray` is a safe, pure
configuration helper returning the canonical compressed 33-byte secp256r1 key.
The argument must be an exact VM ByteString; Buffer, Array and coercible values are rejected.
Only compressed 02/03 encodings of length 33 and uncompressed 04 encodings of
length 65 are accepted. For an uncompressed key, core reconstructs compressed
bytes from X and Y parity, mathematically decompresses that point, then compares
its full uncompressed encoding to all supplied coordinates. Directly decoding
65 bytes and re-encoding them is insufficient because the general ECPoint parser
preserves unchecked uncompressed coordinates. Out-of-field coordinates,
non-residue X values, wrong prefixes and malformed lengths fault. No OS crypto
provider or general ECC behavior is changed. The helper has no account/witness
requirement, no writes and a fixed 32,768 CPU fee; modules use it only when
configuring a key, before writing policy state.
