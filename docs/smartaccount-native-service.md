> ABI 2 supersedes the historical ABI 1 recovery/signature semantics described in
> past receipts below. Current normative rules are in
> [recovery and authority revocation](smartaccount-recovery-epoch.md).

# Native AccountManagement service integration

## Purpose and release boundary

Implement the native SmartAccount profile on the master-n3 integration checkout,
not as an ordinary deployed proxy service. The service has the deterministic
AccountManagement hash, follows TemporaryStorage in native registration, and
requires the independently configured HF_SmartAccountV1 activation. Omission
means disabled. No public-chain configuration is changed.

The draft protocol is the authority for fields, callbacks, delayed changes and
resource limits. Work-in-progress integration is not activation approval. Full
native ABI, execution, composition and persisted NeoExpress scenarios must pass
before a complete implementation or production-readiness claim.

## Architecture and records

The native class owns storage, entrypoint authorization and callback sequencing.
SmartAccountState owns the immutable fourteen-field ABI 2 account record; the protocol
codec owns operation validation, identity and nonce arithmetic. InvocationContext
owns one strongly typed, engine-local state instance for transient account locks
and exact frame grants. Registration and every continuation reuse that instance;
loading a target or resuming a native callback must not recreate the lock table.
The integration uses explicit typed lookup/insertion rather than changing legacy
engine state-factory behavior. ModulePolicy checks current
code and lifecycle ABI, with RFC 8785 binding hashes.

Storage prefixes are scoped to the native contract ID:

- 0x00: profile version and parameter digest, initialized at activation.
- 0x10: canonical account record keyed by accountId.
- 0x11: immutable proxy-address to accountId index.
- 0x20: channel cursor keyed by accountId and 24-byte big-endian channel.
- 0x30: pending module-call record keyed by accountId and role byte.
- 0x40: dependency record keyed by accountId and role byte.

Role bytes are verifier=0 and hook=1. A pending module call stores
`[1, accountId, role, rootBinding, selectedBinding, methodBytes, argumentArray,
proposedAt, matureAt, configurationNonce]`. A dependency record is
`[rootBindingOrNull, cleanupBindings, activeChildHashes]`. All compound fields
are owned canonical snapshots. Pending records require exact Integer version,
role, epoch and UInt64 timestamps; the fixed delay and current root/epoch must
match. The method is a strict UTF-8 ByteString and the argument Array includes
the exact prepended accountId. Queries and confirmation reject malformed stored
records without type coercion; cancellation may delete a malformed intent.
Core-owned child bookkeeping includes bootstrap
children before active-roster publication. Queries return independent copies.

## Entrypoints and errors

Registration checks actual custody witness, identity uniqueness, recovery
separation and module admission before publishing state, reverse index and event.
Lifecycle methods use current ledger time, actual witnesses and the immutable
state transitions, then persist all changes in the native invocation's snapshot.
Every mutating account operation holds the same-account lock before external calls.

Both execution entrypoints take exactly `(accountId, opOrOps, expectedAuthorityEpoch,
expectedConfigurationNonce)`. There is no two-argument overload. The two counters
are exact VM UInt64 Integers and must equal the current record in both Verification
and Application before any callback or nonce update. Transaction witnesses thereby
commit to the same authorization state as operation-digest signatures.

UserOperation execution snapshots all caller input, checks status/deadline/nonce,
validates code, authorizes, consumes nonce, calls hook-pre/target/hook-post/verifier-
post, and emits UserOpExecuted. An exact false target result is not a fault.
Every callback gets an exact frame grant and the prescribed flags/budget. Verification
parses the exact transaction application envelope, validates every operation with
shadow nonces and no writes, and refuses Application-trigger verify calls.

A registered proxy's CheckWitnessInternal path must enforce a current authorized
target frame before any existing fast-path or scope success. Existing Neo witness
rules are additionally required; the grant never creates a witness. Legacy
unregistered addresses are unchanged except for the service identity itself.
After activation, `CheckWitness(AccountManagement.Hash)` is always false: the
permissionless dispatcher is not a user principal, including when it is the
immediate caller. Before activation the existing witness behavior is unchanged.
Custody and nonzero recovery addresses must not be native contract hashes; this
also applies to delayed rotation and stored-record validation. Zero remains the
explicit absence of recovery, not an authority. Transient grants are revoked on FAULT.

Generic module configuration uses the two-step, account-prefixed, manifest-declared
capability in draft sections 8.2-8.3. It is not a generic privileged Contract.Call.
The selected callback, pending intent, epoch and dependencies commit atomically.
Normal errors and gas faults must leave durable state/notifications unchanged.
Before committing configuration, both the selected module and its bound root
must still pass admission against their original pins. A child callback may
call another deployed contract without inheriting its configuration grant; if
that path destroys or changes the root, confirmation must fault and roll back
the child writes, root mutation, cleanup roster, pending intent and epoch.

## Cost and deployment

Ordinary native CPU and storage charges apply; callbacks use the fixed profile
caps. Raw native target dispatch retains exact VM types including Void to Null.
Registry continuations inherit an active composite budget rather than allocating
another full budget under it. Hashing/serialization work must be bounded and metered
at the service boundary; profile constants are not caller-controlled values.

There is no migration of existing ordinary SmartAccounts or public chain restart.
Private tests activate only disposable local chains. Cross-client vectors,
full suite coverage, negative controls, independent review and exact runtime
receipts are release requirements. The full NeoVM/compiler refinement and
cryptographic trust assumptions remain separately reported, not hidden by native
test counts.

## Validation examples

### Negative-path validation requirements

Native admission tests must distinguish protocol rejection from unrelated VM
faults. They check the expected error, absence of published notifications and
unchanged authoritative account/cursor records. The matrix includes missing
custody evidence, non-empty native-fallback signatures, exact verifier Boolean
results, frozen execution, empty/oversized/non-Array batches, direct Verification
without the exact proxy, unknown module roles and missing installed roots.

Stored dependency records are tested independently of callback execution:
noncanonical bytes, wrong VM types/widths, stale roots, over-limit rosters,
duplicates, self-enrollment and active-but-unenrolled children must fail closed.
These are defensive stored-state checks, not a claim that an unprivileged
transaction can directly write native storage. Configuration tests also cover
manifest capabilities, argument bounds, maturity overflow and cleanup capacity.
Active leaf callbacks must exercise real root-to-child dispatch with the correct
account/phase grant; registration or discovery alone is not delegation evidence.

Initialization is checked for exact profile-record bytes and activation gating.
No-block invocations use the authoritative ledger timestamp and must fail if it
is unavailable. These runtime tests do not replace the private-chain lifecycle
matrix, complete VM refinement, or independent review.

- No configured hardfork: AccountManagement has no callable ABI or state.
- Register a custody-authorized empty-module account: deterministic accountId,
  proxy index, Active state, authority epoch zero and configuration nonce zero.
- Propose and activate recovery-address rotation: immature confirmation faults;
  maturity succeeds, advances the epoch and clears all pending intents.
- Submit an expired, repeated or reentrant operation: FAULT and no nonce change.
- A Global-scoped proxy signer outside its target invocation: witness false.
- A child attempts to publish a root roster: FAULT; a root's authorized
  configuration callback can publish only validated leaves.
- Post-callback failure or post-Huyao return charge exhaustion: no persisted
  nonce, target write, module write or notification from the operation.

## Current bounded validation snapshot (2026-10-06)

The current receipt is
`docs/reports/smartaccount-native-source-runtime-validation-20261006.json`.
The full node suite passes 1,702 cases with no failures or skips, including 41
focused native service cases. Entry and execution files reach 100% line/branch
coverage; module management reaches 100% line and 98.46% branch coverage. All ten
new native/component files exceed the 90% line/branch threshold. The tracked
engine integration diff has 96/96 added instrumented lines and 64/66 branch
conditions covered. Enum/attribute metadata without executable sequence points
is not counted. Coverage closes this numeric gate, not full semantic refinement.
The bounded/raw native bridge remains at 100% line and branch coverage.

A reproduced child-configuration defect allowed a callback to destroy its root
and still commit. Confirmation now re-admits the root as well as the selected
module before publishing state. Four core regressions cover destruction and
manifest replacement for both module roles. The English native draft includes
this explicit postcondition; no public proposal was edited in this validation.

The core suite also passes against the exact five core/VM assemblies loaded by
the new source-built private runtime, with byte identity checked after execution.
The previous coverage run measures the same unchanged production source; the new
uninstrumented assembly run is not presented as a separate coverage measurement.

Four clean offline builds (an initial pair and a receipt-pinned replay pair)
compile eleven projects from 611 source files and 50 external package archives.
All 104 runtime files match byte-for-byte, including symbols, dependencies and
native libraries. No old private core/node package or assembly overlay is used.
The build pins the SDK/host profile, source, compatibility patch, recipe, dependency
archives and locks. This establishes same-host reproducibility, not compiler
correctness or an independent security audit of external dependencies.

The private matrices rerun on this exact source-built assembly snapshot cover:

- Seven native lifecycle/execution transactions and three preflight rejections,
  including positive custody and negative service-hash witness controls.
- Six signed proxy transactions, including two persisted callback-ABORT faults,
  plus three distinct admission rejections. Script/witness bytes, signer scopes,
  block inclusion, asset balances and nonce deltas are read back independently.
- Ten configuration transactions and two registrations: destructive verifier
  and hook children fault and roll back root destruction, child storage,
  pending intent, cleanup roster and epoch. Benign controls under the same roots
  then commit an exact false result, storage marker, enrollment and new epoch.
- Twenty-five recovery/authorization transactions plus registration: fifteen
  HALT and ten expected persisted FAULT outcomes. Real joint witnesses enforce
  unfreeze, custody cancellation expires at maturity, and neutral-payer recovery
  replaces custody while preserving account ID, proxy and nonce history. This is
  a historical ABI 1 receipt: current ABI 2 recovery changes the authorization
  domain, detaches all modules/pending intents and advances both counters.
  The retired custody key no longer authorizes execution or joint unfreeze.
- Four current-runtime activation diagnostics, including future, disabled,
  active and exact live activation boundary cases. The historical regression
  remains in its earlier receipt and was not counted again. Diagnostic modules are not certified
  production plugins and these cases do not cover every native protocol path.

The formal host gate passes 20 modules, 262 closed declarations and 130 rejected
semantic mutations. Two-binding postconditions and snapshot selection are
abstract model obligations, not host-runtime or compiler refinement proofs.
Current container replay is unavailable; no daemon was started or changed.

Remaining gates include adversarial conformance beyond the tested matrix, the
full persisted plugin/lifecycle matrix, independent review, complete VM/native-host/compiler
refinement, cryptographic and external-proof assumptions, arbitrary plugin
behavior, current formal-container replay, and independent assessment of the
external dependency and runner compatibility-adapter security. The source-built
runtime closes the previous overlay-only build gap. No current result establishes
arbitrary module availability or prevents externally initiated module changes.
This validation turn changed build/validation tooling and documentation, not
production C# or protocol parameters. Build-tool failure and interruption controls
fail closed; 72 script tests and 4 document tests pass. All work
remains uncommitted; no public network was used.
