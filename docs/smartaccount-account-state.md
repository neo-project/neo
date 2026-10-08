> Current revision: ABI 2. Account records append `AuthorityEpoch` at index 13,
> with record version 2. Mature recovery detaches both module roots and advances
> both counters. See [the current recovery contract](smartaccount-recovery-epoch.md).
> Historical ABI 1 validation receipts below do not establish ABI 2 behavior.

# Native SmartAccount state and lifecycle

## Purpose and boundary

`SmartAccountState` is the native profile's account-record codec and functional
lifecycle state machine. It defines the exact fourteen-field storage value and
the register, delayed-configuration, freeze, and recovery transitions. It is
not a registered native contract. The service must still supply real witness
results, enforce the Application trigger and execution lock, validate module
code/ABI, perform bounded cleanup, commit storage, and emit the declared events.
These requirements are not replaced by this component's tests.

## Canonical state

The state is an exact Array, serialized with Neo's binary serializer:

```text
[2, accountId, accountAddress, custodyAddress, recoveryAddress,
 verifier, hook, status, configurationNonce, pendingVerifier, pendingHook,
 pendingRecoveryAddr, pendingRecovery, authorityEpoch]
```

`Active = 0`, `Frozen = 1`; all other status values are invalid. Addresses are
exact 20-byte ByteStrings; hashes are exact 32-byte ByteStrings. Integers must
have the Integer type, not Boolean or ByteString coercions. The configuration
nonce, authority epoch and timestamps are unsigned 64-bit values. The authority
epoch cannot exceed the configuration nonce. Optional records use Null,
not empty arrays or zero-valued module bindings. A module is `[contract, codeHash]`.
A pending module is `[contractOrZero, codeHashOrZero, proposedAt, activateAt,
expectedConfiguration]`. A pending address is `[address, proposedAt, matureAt,
expectedConfiguration]` with the fixed module or custody-recovery delay.

Deserialization checks full byte consumption, canonical re-encoding, exact
field counts/types, storage-key account binding, deterministic proxy address,
valid authority relationships, proposal epoch and exact fixed delays. Decoding
is capped at 1024 bytes and 64 stack items; every valid version-2 record fits
within those limits. No map, Struct, trailing bytes, oversized integer,
alternate integer encoding, stale pending record, or future schema is accepted.
Malformed stored state raises `FormatException`, never a partial/default record.

Neo hash classes are mutable. The state and module records own copied hash
bytes and return new hash values. Transitions create new state records and do
not mutate their inputs. `ToStackItem` and serialization return fresh values.

## Authority and transition rules

Registration derives identity/address from network, custody and 32-byte salt,
requires custody authorization, and starts Active with configuration nonce and authority epoch zero.
Custody is nonzero and cannot equal the derived proxy. Recovery is zero or
distinct from both custody and that proxy. In particular, self-proxy recovery
would make unfreezing require a witness that the frozen proxy cannot provide.

| Transition | Authority and conditions |
|---|---|
| Propose verifier/hook/recovery address | Current custody; Active; no custody recovery pending; configuration counter not exhausted |
| Activate proposed configuration | Permissionless after its exact delay; Active; no custody recovery pending; matching epoch |
| Cancel proposed configuration | Current custody; proposal present; no active binding change or epoch increment |
| Propose custody recovery | Configured recovery witness; Active or Frozen; new nonzero custody distinct from current custody, recovery and proxy |
| Execute custody recovery | Permissionless after the fixed recovery delay; matching epoch; revoke all modules; preserve account identity/address, nonce history and freeze status |
| Cancel custody recovery | Recovery witness at any time; custody witness only strictly before maturity |
| Freeze | Active account and configured recovery witness |
| Unfreeze | Frozen account and custody witness, plus recovery witness if configured |

Reproposing replaces that pending intent and restarts the full delay. Repeating
freeze/unfreeze in the same status faults rather than advancing the epoch.
No authority is inferred merely because a Boolean argument is true: methods
requiring recovery also require a configured recovery address. Boolean witness
facts are an internal interface to the native runtime, not a public ABI.

Only recovery increments the authority epoch and clears the verifier/hook bindings.
Every successful activation, recovery execution, freeze or unfreeze increments
the configuration nonce and clears **all** pending intents. This makes epoch
invalidation explicit and avoids stale recovery records blocking configuration.
There is no counter or timestamp wraparound. Exhausted counters reject new
proposals and state transitions. Time addition is checked before making a
proposal; maturity uses `now >= matureAt`.

Configuration changes are blocked while custody recovery is pending. Otherwise
an old custody holder could invalidate a mature recovery via a configuration
epoch increment even though direct cancellation after maturity is forbidden.
Configuration cancellations remain possible because they neither change a
binding nor advance the epoch. Recovery-address configuration is also blocked
while Frozen: removing recovery and then unfreezing with custody alone must not
bypass the joint-witness freeze rule. These gates also apply to root/child
module configuration when that native route is integrated.

## Callbacks, errors and atomicity

Module records validate local identity shape and reject native contracts; actual
deployment, policy blocking, lifecycle ABI, code-hash matching, discovery, and
cleanup require the native runtime. A calculated replacement state must not be
persisted before all required bounded callbacks succeed. Recovery is the explicit
exception to module cleanup: it calls no module and removes native dependency
registries atomically with the new authority epoch. Callback failure must
fault the outer invocation, preserving the original account, child state,
dependency registry and notifications. A pure state-machine return value is not
proof of NeoVM transaction rollback.

Invalid authority, missing proposal, forbidden status, pending-recovery conflict,
immature intent, or exhausted counter/time arithmetic raises
`InvalidOperationException`. Invalid structural/address input raises
`FormatException`; null host API objects raise `ArgumentNullException`.

## Example and deployment

From an Active account with configuration nonce zero, proposing a verifier at
time 1000 produces maturity 86,401,000 and does not change the active verifier.
Activation at 86,400,999 fails. Activation at 86,401,000 changes the binding,
sets configuration nonce one and clears every pending intent. The original
record and independently stored operation nonce cursors are unchanged.

These rules refine an unactivated draft. No public-chain deployment or storage
migration is performed. Before native activation, the complete ABI, events,
authority sourcing, callbacks, storage atomicity and private NeoExpress
activation/readback must be verified. Existing deployed-contract foundation
storage must not be reinterpreted as this native record format.

## Reproducible checks

```sh
python3 tests/Neo.UnitTests/SmartContract/Native/TestFile/generate-smartaccount-state-vectors.py --check
dotnet test tests/Neo.UnitTests/Neo.UnitTests.csproj --no-restore --filter FullyQualifiedName~UT_SmartAccount
```

The independent Python fixtures cover an initial record, all four pending
intents, the exhausted UInt64 epoch, and native-witness fallback after verifier
removal. Node tests compare exact bytes, not only deserialized field values.
Private NeoExpress checks execute their typed VM initializers, serialize them
through StdLib, and independently deserialize/serialize each stored value.
These RPC simulations do not call or activate native AccountManagement.

The companion protocol repository's `NativeLifecycle.v` proves authority-guard
and epoch-invalidation properties in an abstract model, with semantic mutations
that must invalidate proofs. It assumes witness facts and does not establish
mechanized C# refinement, cryptographic security, callback cleanup or ledger
atomicity. Compiled C# mutation tests independently test corresponding guards;
passing both is complementary evidence, not a theorem relating the two programs.

The current component snapshot is recorded in
[`smartaccount-state-validation-20261006.json`](reports/smartaccount-state-validation-20261006.json).
Earlier wire/envelope receipts describe earlier source snapshots; their source
hashes must not be treated as evidence for later edits. The current receipt
separates component tests, host proofs, private RPC simulations, unavailable
container checks, and still-open native service integration.
