# Native SmartAccount callback dispatch

## Runtime integration requirement

Native AccountManagement must invoke verifier, hook and maintenance callbacks
with explicit flags and independently bounded gas. The existing
`CallFromNativeContractAsync` path selects All and creates no child budget;
inheriting an existing budget is not sufficient to originate a verifier cap.
Calling the public bounded syscall handler without registering a native awaiter
would also leave the native continuation and return value unmanaged.

## Internal API

`CallFromNativeContractWithGasLimitAsync(callingScriptHash, contractHash,
method, flags, gasLimit, args)` returns `ContractTask<StackItem>`. It shares the
bounded syscall's context creation, parent-budget checks, whitelist accounting,
initialization and dynamic-return handling. Void returns Null; other results
retain their exact VM type. A Boolean verifier result must therefore be checked
as Boolean by the native protocol, not coerced from an Integer or ByteString.

The caller must be the currently executing native contract. The method requires
explicit SmartAccount hardfork activation and the caller's ReadStates and
AllowCall permissions. Requested flags cannot elevate the caller's permissions;
safe manifest methods further remove writes and notifications. It charges the
bounded-call interop dispatch fee once to the caller before allocating the
child budget; the C# entry adds no synthetic VM push-instruction fees.

The budget covers initialization, descendants, return instructions and existing
native asynchronous callbacks. Return resumes the native continuation in its
saved caller context, not in a child with residual privileges. Child storage
and notifications remain within the caller's snapshot boundary. A VM fault,
including a post-Huyao return-instruction charge fault after continuation work,
must fault the outer invocation and must not publish its state or notifications.
Consumed fees are not rolled back.

## Errors, tests and compatibility

Missing activation, a non-native/mismatched caller, missing permissions,
invalid flags, nonpositive or excessive limits, missing/blocked contracts and
invalid methods fail before executing callee bytecode. The usual VM fault and
native continuation behavior applies once a callback is scheduled. The raw
StackItem result is intentional; ABI and protocol-result validation belong to
the caller and may not be silently delegated to coercive host conversion.

Tests exercise both pre- and post-Huyao charging, ReadOnly enforcement, exact
return types, caller identity, initialization, LoadScript/CALLT descendants,
GAS native callbacks, ancestor budgets, fee whitelists, continuation ordering,
and storage/notification rollback. Shared syscall tests must remain green.
Existing unbounded native-call methods are unchanged; no public ABI, native
contract registration, network activation height or deployed artifact is added
by this engine bridge alone. A complete AccountManagement implementation must
still use this bridge for every protocol callback and undergo private-chain
activation and lifecycle verification.

### Height and fee-attribution boundaries

For SmartAccount specifically, an engine without a PersistingBlock must evaluate
activation against the current ledger height, not merely the presence of a
configuration key. An explicit future height is still disabled. A supplied
PersistingBlock uses its index, including exact activation equality. Other
hardforks retain their existing no-block compatibility behavior.

The native continuation may execute while `_whitelisted` still describes the
returning child's instruction. New native dispatch must temporarily select its
current native caller's whitelist state for the dispatch fee and context setup,
then restore the instruction state. Otherwise a whitelisted child can exempt
the next native dispatch, or an unwhitelisted child can charge an exempt caller.
The restoration is also required on setup failure so post-Huyao RET charging
still uses the returning child's original fee policy and bounded budget.

## Remaining service integration requirements

The bridge alone is not AccountManagement. The current integration now includes
the registered native service, activation/profile storage, delayed configuration
and the core-owned cleanup roster. Their normative interfaces and canonical
records are specified in the companion native draft. Native service testing and
its remaining conformance gates are recorded in `smartaccount-native-service.md`;
component-level bridge evidence does not close the full service profile.

The prior GAS callback test exhausted its budget in the native transfer entry
fee before reaching `onNEP17Payment`. Its corrected budget now pays the real
transfer cost, and the test requires the faulting script hash to equal the
recipient callback. A generic budget-exhaustion exception alone is insufficient
coverage evidence for native callback inheritance.

## Reproduction and evidence

Run the engine regressions from this repository:

```sh
dotnet test tests/Neo.UnitTests/Neo.UnitTests.csproj \
  --filter 'FullyQualifiedName~UT_ApplicationEngine_NativeBoundedCall|FullyQualifiedName~UT_ApplicationEngine_BoundedCall'
```

Expected results include exact Boolean/Integer/Null return preservation,
future-height rejection, successful execution at the activation height, and
FAULT with no published state or notifications on callback-budget exhaustion.
The full node suite must also pass; the focused filter alone is not a release
gate.

`docs/reports/smartaccount-native-callback-validation-20261006.json` binds the
reviewed source snapshot to full-suite results, method-level coverage, compiled
source mutations and companion formal/private-chain receipts. The companion
repository's `scripts/neoexpress_activation_validate.py` accepts a local runner
assembly directory, creates only disposable private chains and independently
checks the activation regression and exact live height boundary. Its receipt
does not certify invocation of this internal helper: that is covered by engine
tests, including real GAS native continuations, until AccountManagement itself
is integrated.

## Raw dispatch and inherited-budget continuation

`CallFromNativeContractRawAsync(caller, target, method, flags,
requireInheritedBudget, args)` is used for a UserOperation target and for a
registry continuation already beneath a capped root. It never creates a new
budget object. A required but absent inherited budget fails before scheduling.
Every existing ancestor remains charged, including the dispatch fee, callee
initialization, dynamic LoadScript, and return. Without an inherited callback
budget, normal transaction gas is still enforced.

The raw path must retain the same hardfork, native caller, permissions, safe
method, exact-type return, native caller identity, whitelist restoration and
atomicity constraints as the bounded bridge. Explicit inherited-budget success
and admission rejection tests cover this separate route; passing bounded-helper
tests alone is not evidence for its dispatch implementation.

Raw-dispatch regressions additionally require actual initializer, LoadScript,
CALLT and native GAS recipient execution beneath the same ancestor budget.
For the GAS case the faulting script must be the recipient, with sender and
recipient balances and notifications rolled back. A blocked target dispatched
from a resumed native continuation must charge the caller's dispatch fee and
restore the returning child's whitelist policy even when setup throws.
Non-null diagnostics must receive the raw dispatch exactly once.
