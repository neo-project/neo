# SmartAccount ABI 2 verification envelope

## Purpose and integration boundary

The account proxy must not authorize arbitrary transaction scripts. Before
any verifier is called, the native service must decode exactly one canonical
`executeUserOp` or `executeUserOps` call to `AccountManagement` for that account.
`SmartAccountEnvelope` implements this state-independent boundary. It never
executes input bytecode, invokes a contract, or accesses storage.

This is not a native service registration or a complete witness verifier.
The native `verify` entrypoint additionally enforces the Verification
trigger, the proxy caller/address binding, active account state, deadline,
module identity, bounded signature validation, custody witness fallback, and equality
of the committed authority epoch/configuration nonce with the current account record.
Application execution must repeat authorization and enforce atomic state
transitions. Those obligations must not be inferred from parser acceptance.

## Exact canonical grammar

The complete script contains exactly the following inert initializers and call:

```text
PUSH expectedConfigurationNonce
PUSH expectedAuthorityEpoch
INITIALIZE op or ops
PUSH accountId
PUSH 4
PACK
PUSH All
PUSH "executeUserOp" or "executeUserOps"
PUSH AccountManagement.Hash
SYSCALL System.Contract.Call
```

The resulting argument Array is `[accountId, opOrOps, expectedAuthorityEpoch,
expectedConfigurationNonce]`. Both counters must be exact VM Integers in the
UInt64 range. The historical two-argument call is rejected, including when its
operation has a valid signature. There is no additional prefix or suffix. The
typed initializer is recursive:

- Null: `PUSHNULL`.
- Boolean: `PUSHT` or `PUSHF`, not an Integer followed by a conversion.
- Integer: the shortest encoding produced by `ScriptBuilder.EmitPush`.
- ByteString: the shortest length-prefixed data push produced by that builder.
- Empty Array/Struct: `NEWARRAY0`/`NEWSTRUCT0`.
- Non-empty Array/Struct: initialize children in reverse order, push their
  count canonically, then `PACK`/`PACKSTRUCT`.

The operation and top-level argument list must still be Arrays, not Structs.
Struct is supported only as an argument value. Generic SDK overloads which
encode an empty Array as `PUSH0 PACK` do not produce this profile's canonical
initializer. Type-preserving initializers are required; converting all values
to generic `ContractParameter` objects can lose Struct information.

## API and errors

`CreateApplicationScript(accountId, payload, isBatch, expectedAuthorityEpoch, expectedConfigurationNonce)` accepts one six-field
operation or an Array of 1 through 32 operations and returns fresh script bytes.
It validates every operation before encoding. Every operation implicitly uses
the one account ID in the envelope; a batch cannot supply per-operation IDs.

`Parse(accountId, script)` matches the exact native/account/method/flag/syscall
suffix and exact four-field argument Array, decodes only inert typed initializers,
validates operation bounds and unsigned counters, and
requires byte equality with canonical re-encoding. Malformed, noncanonical,
oversized, executable, truncated, or mismatched scripts raise `FormatException`.
Inputs exceeding Neo's maximum transaction size are rejected before parsing.
The full transaction, including attributes and witnesses, must separately pass
normal transaction-size validation.

The returned envelope owns serialized snapshots of its account identity and
operations. Neo hash objects expose deserialization methods and are mutable;
neither input hash objects nor returned hash objects may alias internal identity.
`GetOperation(index)` returns a fresh decoded value, so mutation of the input
script, input operation, or a previously returned operation cannot retarget a
later authorization check. An invalid index raises `ArgumentOutOfRangeException`.

`ValidateNonces(readCursor)` reads each channel's stored cursor once and checks
operations in array order against a private shadow cursor. It does not mutate
the supplied storage. A batch with sequence 0 followed by 1 is valid when the
stored cursor is 0; checking both against that unchanged 0 would incorrectly
reject a valid batch. Gaps, duplicates, exhaustion, and malformed cursor values
retain the nonce codec's fail-closed behavior. This is nonce validation only,
not authorization or a state commit.
Null host-language API arguments raise `ArgumentNullException`; they are not
alternative VM encodings.

## Resource and security model

The decoder is an explicit stack walk, not recursive interpretation. It rejects
unknown opcodes and bounds constructed depth to the maximum implied by the
outer call-argument Array, batch,
operation, argument-list, and eight nested argument containers. Every byte is
processed a bounded number of times. The script-size cap bounds transient
allocation even for malformed inputs; validated operation limits bound retained
data. No `DUP`, `DROP`, branch, call, `CONVERT`, or arbitrary syscall is evaluated.

Native integration must charge bounded parser work according to the native gas
schedule; this pure component neither charges nor exempts any execution fee.
The parser does not relax NeoVM stack limits or guarantee that a syntactically
valid transaction fits the remaining Verification gas envelope.

## Verification and deployment

Tests must include both single and batch envelopes, exact published identity
bytes, all supported value types, Integer/data-push boundaries, malicious and
noncanonical scripts, truncation, deep containers, copied-result isolation,
and shadow-cursor batch validation. Differential tests evaluate only the inert
initializer in the actual Neo VM and compare its value with the parser result.

No existing deployment, native ID, or activation setting is modified by this
component. No mainnet or testnet action is required or authorized. A private
NeoExpress test of inert initializer bytes is runtime evidence for that subset,
not proof of native service activation or a complete VM/compiler refinement.

The historical ABI 1 receipt is
[`smartaccount-envelope-validation-20261006.json`](reports/smartaccount-envelope-validation-20261006.json).
Its old two-argument execution bytes are not valid under current ABI 2.
It records 104 focused tests, 1556 current-branch tests, 1566 latest-master
overlay tests, and five compiled source mutations rejected by the tests.
Envelope-component line/branch coverage is 99.43%/99.05%; the wire codec remains
at 100%/100%. The current host formal gate passed 17 modules, 209 closed
declarations and 98 semantic mutations. The changed shadow-cursor proofs have
not been rerun in the pinned container because its local daemon was unavailable;
the previous container result is not claimed for the new source bytes.

## Transaction witness revocation

Operation-digest signatures and standard Neo transaction witnesses have distinct
signing inputs. Custody fallback and NeoNativeVerifier may authorize only through
the transaction witness. Committing both counters in the unsigned transaction
script makes those signatures expire across account configuration and recovery,
even if the same custody is restored and the operation nonce remains unused.

Verification checks the commitment before callbacks and shadow nonce validation;
Application repeats it before callbacks or nonce writes. A stale raw transaction
with the proxy witness fails admission; a transaction using only an external
custody/payer witness can reach Application and faults there. Neither path may
refresh the signed script or reset a nonce lane. The exact stale-state error is
`The SmartAccount execution authority epoch or configuration nonce is stale.`

The state-independent codec does not establish that a commitment is current.
The native service obtains current state and applies that final comparison.
