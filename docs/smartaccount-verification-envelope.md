# SmartAccount verification envelope

## Purpose and integration boundary

The account proxy must not authorize arbitrary transaction scripts. Before
any verifier is called, the native service must decode exactly one canonical
`executeUserOp` or `executeUserOps` call to `AccountManagement` for that account.
`SmartAccountEnvelope` implements this state-independent boundary. It never
executes input bytecode, invokes a contract, or accesses storage.

This is not a native service registration or a complete witness verifier.
The eventual `verify` entrypoint must additionally enforce the Verification
trigger, the proxy caller/address binding, active account state, deadline,
module identity, bounded signature validation, and custody witness fallback.
Application execution must repeat authorization and enforce atomic state
transitions. Those obligations must not be inferred from parser acceptance.

## Exact canonical grammar

The complete script consists of a typed initializer for `op` or `ops`, followed
by exactly these instructions:

```text
PUSH accountId
PUSH 2
PACK
PUSH All
PUSH "executeUserOp" or "executeUserOps"
PUSH AccountManagement.Hash
SYSCALL System.Contract.Call
```

There is no prefix or suffix. The typed initializer is recursive:

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

`CreateApplicationScript(accountId, payload, isBatch)` accepts one six-field
operation or an Array of 1 through 32 operations and returns fresh script bytes.
It validates every operation before encoding. Every operation implicitly uses
the one account ID in the envelope; a batch cannot supply per-operation IDs.

`Parse(accountId, script)` matches the exact native/account/method/flag/syscall
suffix, decodes only inert typed initializers, validates operation bounds, and
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
unknown opcodes and bounds constructed depth to the maximum implied by batch,
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

The current receipt is
[`smartaccount-envelope-validation-20261006.json`](reports/smartaccount-envelope-validation-20261006.json).
It records 104 focused tests, 1556 current-branch tests, 1566 latest-master
overlay tests, and five compiled source mutations rejected by the tests.
Envelope-component line/branch coverage is 99.43%/99.05%; the wire codec remains
at 100%/100%. The current host formal gate passed 17 modules, 209 closed
declarations and 98 semantic mutations. The changed shadow-cursor proofs have
not been rerun in the pinned container because its local daemon was unavailable;
the previous container result is not claimed for the new source bytes.
