# Native SmartAccount invocation authority

## Purpose

The native service requires more than an account-wide busy flag or the hash of
an allowed contract. Callback authority must identify an account, module kind,
phase and actual call frame. A target contract can also be a configured module;
that coincidence must not turn a target invocation into a configuration or
post-execution callback. Calling an authorized contract again must not inherit
authority from an earlier suspended invocation of that same contract.

`SmartAccountInvocationContext` supplies engine-local locks and scoped grants.
It is used through `ApplicationEngine.GetState` by the native service, never
serialized into ledger storage. It does not establish a witness, validate code
identity, admit plugins or register AccountManagement. Those obligations remain
with the complete native service.

## Scope and internal API

`EnterAccount(accountId)` obtains a same-account lock and derives the immutable
account-address identity. Reentry for that account fails before external code.
Different accounts may nest, but their grants remain distinct.

`EnterModule(accountId, kind, module, phase, context, children)` establishes a
module grant anchored to the callee's `ExecutionContextState`. The context must
identify a loaded contract with the expected script hash. Initialization and
internal VM calls share that state; a fresh cross-contract call does not.
The phases are Validation, PreExecute, PostExecute, Configuration and Cleanup.
Hooks cannot receive Validation; verifiers cannot receive PreExecute.

Only execution phases may delegate to the supplied validated leaf roster.
Verifier rosters contain at most ten children and hook rosters at most eight.
Zero, self, duplicate and native child identities are rejected. Authorization
requires the queried module to be the actual caller of AccountManagement and
either the anchored root itself or a roster child called directly by that root.
Unregistered children, grandchildren, recursive root calls and LoadScript
contexts do not acquire a grant. Configuration and cleanup use individual
direct grants for each child, not a root-wide delegation.

`EnterTarget(accountId, target, context)` grants only the exact loaded target
invocation. It is not a module grant. `IsTargetAuthorized` is for a query into
AccountManagement; `IsWitnessAuthorized` examines the currently executing
target directly. Neither grants authority to arbitrary target descendants,
even if a transaction signer has Global scope. They are additional constraints,
not replacements for the real transaction witness and scope checks. The native
service must connect the latter check to registered SmartAccount proxy witnesses.

`IsModuleAuthorized`, `IsTargetAuthorized` and `IsWitnessAuthorized` return
false outside their active frame or for a different account, role or phase.
Public callers do not supply frame identities; the query paths derive them
from the engine's actual calling context. Public context queries must require
Application trigger for mutable callback phases, and only Validation may be
queried during Verification. The native caller must select valid triggers
before installing a grant.

## Lifetime, rollback and errors

Scopes release in reverse order and Dispose is idempotent after successful
release. Releasing an account before its callback, or a suspended frame before
its nested frame, raises InvalidOperationException without changing authority.
Account identities, target/module hashes and child identities are copied;
mutating caller-owned hash objects cannot rebind a live grant.

ApplicationEngine.OnFault clears all grants and locks. Already-issued leases
become inert, so late disposal cannot remove a newer invocation's grant or
resurrect an earlier authorization. VM faults do not depend on a pending native
async continuation reaching its finally block. Ledger rollback remains the
engine snapshot's responsibility; clearing these ephemeral grants is separate.

Malformed identities, kinds, phases and rosters are rejected before installing
any state. Grant installation without a locked account is rejected. Resource
use is linear in the bounded roster, constant for direct-frame checks, and
bounded by the engine's invocation limit for nested account scopes.

## Example and verification

For account A, a verifier Validation grant cannot authorize that verifier's
PostExecute method. A listed verifier child directly called by the root may
query the Validation grant, but the same child reached through an intermediary
must be rejected. After that grant is disposed, the target phase may authorize
A's asset witness only in the saved target call frame, not in its descendants.
A fault removes all grants even when a native continuation remains pending.

Tests must include exact frame identity, initialization clones, same-hash new
calls, role/phase/account separation, depth-one delegation, target isolation,
mutable-hash aliases, nested scope ordering, reset and real engine FAULT cleanup.
These checks are necessary native integration evidence, not a complete native
service or full NeoVM refinement certificate. No network activation or ordinary
contract migration is performed by this engine-local component.
