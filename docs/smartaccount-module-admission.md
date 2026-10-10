# SmartAccount module admission and code identity

## Purpose and status

The native profile binds deployed module bytes, not a source filename or a
manifest marker. A recursively sorted JSON description alone is ambiguous for
Unicode escaping, negative zero and exponent notation. This component fixes the
encoding and validates the lifecycle ABI before any discovery callback.

This is an internal native-service dependency, not a registered AccountManagement
service. It does not establish plugin honesty, key independence, orphan-free
cleanup, or whole-program refinement. Activation, custody authorization, storage,
configuration delays, execution and cleanup orchestration remain service work.

## Encoding

`CanonicalManifestJsonUtf8` is RFC 8785 JCS applied to the full deployed manifest:
https://www.rfc-editor.org/rfc/rfc8785.html

Property names use unsigned UTF-16 lexical order, including names inside arrays;
array order and Unicode normalization forms remain unchanged. Strings use JCS
escaping and strict UTF-8. Numbers use finite binary64 ECMAScript serialization;
negative zero is `0`. Lone surrogates, cycles, excessive nesting and invalid
numbers are rejected, never repaired. The existing Neo JSON nesting limit is 64.
The existing native manifest size bound also applies to canonical bytes.

The binding digest is SHA-256 of the complete serialized NEF (including header,
tokens and checksum), one zero byte, and those canonical manifest bytes. No
source-language or deployment-transaction metadata replaces any of these bytes.
For example, `{"z":-0,"a":1e-6}` canonicalizes to
`{"a":0.000001,"z":0}`. Reordering an object does not change a binding; changing
array order, permissions, metadata or NEF bytes does.

## Internal interface

- `SmartAccountCanonicalJson.Serialize` produces owned canonical UTF-8 bytes.
- `SmartAccountModulePolicy.GetCodeHash` computes a binding digest.
- `SmartAccountModulePolicy.Inspect` checks deployment, non-native identity,
  blocking policy, role and exact lifecycle signatures; optionally rechecks a
  stored binding. It returns only an immutable binding, not mutable contract state.
- `DiscoverAsync` calls `supportsComposition` through the bounded native bridge
  with ReadOnly and the fixed maintenance budget. Only an exact VM Boolean is
  accepted. A leaf must also expose the safe signer-domain ABI when a verifier.
- `ReadSignerDomainsAsync` is a separate, read-only, bounded leaf-verifier query
  with exact Array/ByteString types and non-empty distinct 32-byte commitments.
  It does not assume a configured root has a usable signer set.

Inspection and every discovery query recheck current code identity and policy.
Ordinary verifier validation accepts ReadOnly regardless of the ABI safe bit.
Composite verifiers require non-safe `validateCompositeSignature(Hash160, Array)
-> Array` and `postExecuteComposite(Hash160, Array, Any, Array) -> Void`; native
validation still runs ReadOnly. Exact profile digest and Boolean composite
metadata are part of admission and pinned code identity. Verifier discovery must
match that Boolean. Execution selects the pinned capability without invoking a
maintenance-budget discovery callback during Verification. The receipt lifetime
and limits are specified in [composite receipts](smartaccount-composite-receipts.md). The profile
requires clearAccount to be non-safe and discovery to be safe; it does not impose
a safe-bit value on preExecute or postExecute. Normal Neo safe-method restrictions
still apply even when the requested callback flags are All.
Duplicate name/arity descriptors are rejected instead of relying on lookup order.
A composition Boolean does not certify a MultiSig or MultiHook implementation.
Callers must enforce the supported composite profile and cross-child disjointness.

## Errors, cost and trust

Admission failures throw before publishing state. VM callback faults propagate;
no truthiness coercion, exception-to-success conversion, or stale identity cache
is used. The maintenance cap is 250,000,000 datoshi. Callers must still supply a
native invocation with sufficient enclosing gas. Descendants inherit the budget.
Canonicalization sorts each object's members (O(n log n)); hashing is linear in
serialized bytes, and recursive traversal is bounded by the JSON nesting limit.
Read-only discovery has no configuration or asset-authority grant.

The number formatter depends on the supported .NET runtime's shortest-roundtrip
binary64 conversion. RFC vectors and deterministic V8 differential vectors test
this dependency; they are not a proof for every binary64 value. Cryptographic
collision resistance remains an explicit assumption.

## Validation, migration and deployment

Tests are written before implementation. Gates cover exact ABI defects, code
updates, blocked/missing/native modules, canonical strings/numbers/order, and
real NeoVM discovery callbacks with wrong types, writes and faults. A Node.js
fixture generator independently serializes binary64 values with JSON.stringify.
Full native-unit tests and changed-code coverage must pass on the latest-master
integration checkout. Source mutation controls must fail independently.

The draft's previous canonical-JSON description was underspecified, not an
activated network encoding. Regenerate candidate binding hashes with this
encoding; do not reinterpret a live binding silently. No public deployment or
network activation is authorized by these changes. No existing native JSON
serializer or non-SmartAccount contract behavior is changed.
