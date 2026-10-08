# SmartAccount native wire-format boundary

## Purpose and status

The hardfork-gated native service computes identity, authorization bytes and nonce
keys with `SmartAccountProtocol`. ABI 2 separates the stable identity version 1
from authorization version 2. The pure codec requires explicit unsigned 64-bit
authority epoch and configuration nonce; native getters obtain them from the
registered account record. No public network activation is implied. See
[recovery and revocation](smartaccount-recovery-epoch.md) for the breaking draft
revision and its module namespace dependency.

The reference is the unnumbered profile associated with proposals issue #242
and foundation PR #243. Neither identifier is a NEP number.

## Inputs, outputs, and errors

All hash parameters use Neo wire byte order. The native service identity is
`Helper.GetContractHash(UInt160.Zero, 0, "AccountManagement")`. Account creation
uses the profile/version domain, network magic in little-endian order, service
hash, nonzero custody hash, and exactly 32 salt bytes. The account address is
the hash of a canonical `verify(accountId)` dynamic call using `ReadOnly`.

The operation is an exact VM Array (not Struct) with six fields:

| Index | Exact VM type | Constraint |
|---|---|---|
| 0 | ByteString | Nonzero 20-byte target |
| 1 | ByteString | Strict UTF-8, 1 through 128 bytes |
| 2 | Array | At most 64 top-level arguments; at most 4096 serialized bytes |
| 3 | Integer | `0 <= nonce < 2^255` |
| 4 | Integer | `0 <= deadline < 2^255` |
| 5 | ByteString | At most 1024 bytes |

NeoVM uses signed 256-bit Integers. An unsigned 256-bit nonce cannot use this
ABI unchanged: values at or above `2^255` require a 33-byte signed encoding.
There are 191 channel bits and 64 sequence bits, not 192 channel bits. The
channel key remains 24-byte unsigned big-endian, with its highest bit zero.
The sequence cursor allows `2^64` only as a stored exhaustion sentinel; it is
never a usable sequence. No narrowing, wraparound, or sign reinterpretation is
permitted. Deadline comparison and authorization are not performed by this
pure codec.

The top-level argument Array is not a nesting level. Nested Arrays and Structs
count one level each, up to eight. Null, Boolean, Integer, and ByteString leaves
are supported. Buffer, Map, Pointer, and InteropInterface are rejected. A
repeated reference to a compound value, including cycles, is rejected, matching
Neo's binary serializer; callers can use independent equal-valued arrays.

Validation rejects malformed input with `FormatException`. Nonce exhaustion
and a mismatch against a valid stored next sequence use `InvalidOperationException`.
No type coercion from ByteString or Boolean to Integer is permitted. The
unsigned operation serialization replaces the signature with an empty
ByteString only after validating the original signature. Inputs are not mutated.
Returned byte arrays are fresh values, not aliases of caller-owned buffers.

## Canonical example

For network `0x12345678`, custody wire bytes
`202122232425262728292a2b2c2d2e2f30313233`, and salt
`404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f`:

- account ID: `0x3e25330008563c55fe2853e07868b36ca00020ac`;
- proxy address: `0x7829f40af6380c00110932c9551ece0916fdcad1`;
- the ABI 2 transfer-shaped operation digest at authority epoch 7 and configuration nonce 11:
  `cef49fdbc140c1f4fa6d62a9dbfabe86f8f7815f27f1e5f6511fb49a3583ca65`.

This is an encoding vector, not a claim that the named target exports
`transfer` or that this operation can execute successfully.

## Resource and security considerations

Argument traversal charges the exact serialized byte size before descending.
Depth is capped at eight, so cycles and large fan-out cannot induce unbounded
recursive traversal. Work is linear in accepted serialized size, bounded by
4096 bytes, plus fixed operation fields. Oversized leaves are rejected before
serialization copies them. Shared compound references are rejected by identity,
not by an expensive structural equality comparison.

Domain separation commits to the network, native service, account ID, authority
epoch and configuration nonce. The suffix contains the two counters as fixed
8-byte unsigned little-endian values, in that order.
Only signature bytes are excluded from the operation digest. The component
does not verify cryptography, witnesses, module code identity, recovery,
rollback, or full NeoVM semantics. Tests and byte agreement are not a
mechanized compiler or VM refinement proof.

## Integration, migration, and deployment

ABI 2 is a breaking revision of an unactivated draft. Existing ordinary-account
contracts are not migrated. Old native ABI 1 records, modules and signatures are
not accepted as ABI 2; a private prototype chain must be recreated or explicitly
migrated before testing. Identity derivation and its published proxy vectors are
unchanged, but every operation domain/digest changes. Historical validation
receipts below concern ABI 1 and cannot establish ABI 2 closure.

The next native-service integration must preserve all upstream native IDs,
including the newly introduced `TemporaryStorage`, and remain disabled unless
`HF_SmartAccountV1` is explicitly configured. Before a PR, the complete native
ABI, lifecycle semantics, latest upstream baseline, independent review, and
private NeoExpress activation/readback must be verified together.

## Verification record

The local receipt is
[`smartaccount-wire-validation-20261006.json`](reports/smartaccount-wire-validation-20261006.json).
It records 54 focused tests, 1506 passing tests on the current branch, 1516
passing tests on an isolated latest-master overlay, and five compiled source
mutations rejected by the runtime tests. The initial unrelated P2P test failure
and its clean focused/full reruns are retained in the receipt, not discarded.

The companion protocol repository contains the assumption-audited
`NativeIntegerDomain.v` model, host/container gate receipts, numerical vectors,
and five private NeoExpress Integer-boundary simulations. The simulations use
the existing private runner and `StdLib.deserialize`; they do not exercise this
new codec through a callable native SmartAccount service. Public deployment is
not a requirement for this local verification, and no public chain was touched.

## Canonical execution ABI 2

`executeUserOp(accountId, op, expectedAuthorityEpoch, expectedConfigurationNonce)`
and `executeUserOps(accountId, ops, expectedAuthorityEpoch, expectedConfigurationNonce)`
accept exactly four parameters with ABI types Hash160, Array, Integer, Integer.
Counter values must be exact VM Integers in `0..2^64-1`; Boolean/ByteString
coercion is forbidden. Both values are committed by the standard transaction
signature because they occur in its unsigned script. They do not add fields to
the six-field UserOperation or change identity version 1.

[The envelope grammar](smartaccount-verification-envelope.md) requires canonical
pushes in reverse parameter order and a four-element `PACK`. Verification and
Application independently compare the supplied values with current state.
Historical v1 envelope vectors remain historical fixtures only; current
`smartaccount-envelope-v2.json` uses the distinct counters 7 and 11. Tests reject
the old two-argument wire format. The superseded preliminary ABI 2 two-argument
draft is also incompatible; private runtime/artifact sets must be rebuilt together.

The current profile parameter digest is
`4201b02f571b7415121467d67343a8189b8070ad795a82424c0403782d22b1b4`.
