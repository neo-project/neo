# Native SmartAccount ABI 2: recovery and authority revocation

Status: proposed implementation contract, 2026-10-08. The original dirty prototype is preserved at `/Users/jinghuiliao/git/neo-os/.worktrees/smartaccount-native-integration` (base `06e9058c1606dcc9e163664916f4fe18abc470e7`). Its uncommitted files are part of the implementation identity. Work proceeds only in the independent convergence snapshot. No public network has activated this revision.

## Problem and decision

The previous draft deliberately retained verifier and hook bindings during custody recovery. Normal module replacement first called every enrolled child and root `clearAccount`; a faulting, deleted, blocked, or updated module could therefore permanently prevent replacement even after recovery. `ConfigurationNonce` protected pending intents but was absent from the signing domain. A still-unused signature could remain valid after recovery or a policy change when the same verifying authority remained installed.

ABI 2 changes the existing `executeRecovery(accountId)` semantics. No second emergency endpoint is added. After the existing recovery witness proposal and seven-day delay, execution atomically increments both counters, changes custody, detaches verifier and hook, deletes both native dependency registries and module-call intents, and clears lifecycle intents. It calls no old or new module, including discovery or cleanup. Identity, proxy, recovery address, all nonce-channel cursors and Frozen/Active status remain unchanged. Cancellation/maturity rules and same-account transition locking remain enforced. Frozen accounts require the new custody plus the configured recovery witness to unfreeze.

Ordinary module replacement and child removal continue to require successful cleanup and transaction rollback on failure. Recovery is an explicit authority revocation, not a claim that remote module storage or external approvals have been erased. Recovery requires a configured recovery authority; an Active custody holder can first configure one through the existing delay, even when an installed module is faulty. No new custody-only bypass of Frozen is introduced.

## Versions and ABI

Identity derivation stays version 1 so its account/proxy vectors are unchanged. State records, native ABI and authorization domain become version 2. This is a breaking revision of an unactivated draft, not an automatic production migration. ABI 1 modules, records, signatures and clients must not silently fall back or mix with ABI 2 artifacts.

- `getVersion() -> Integer`: 2.
- `getAccount(accountId) -> Any`: Null for unknown account; otherwise an exact 14-item Array. Existing indexes 0..12 retain meaning, index 0 is record version 2, index 13 is unsigned 64-bit `AuthorityEpoch`. Registration initializes both counters to zero.
- `getAuthorityEpoch(accountId) -> Integer`: safe, ReadStates; faults for unknown account.
- `getAuthorizationDomain(accountId) -> ByteArray`: safe, ReadStates; faults for unknown account and reads both counters.
- `getOperationDigest(accountId, op) -> ByteArray`: same arguments, state-bound ABI 2 digest.
- `RecoveryExecuted`: `[accountId, oldCustodyAddress, newCustodyAddress, configurationNonce, authorityEpoch]`.
- No recovery/configuration mutation gains an extra parameter. Existing getters, six-field UserOperation shape and two-dimensional nonce encoding remain.

The exact signing preimage is:

`ASCII("NeoSmartAccount/UserOperation") || 0x02 || networkLE32 || nativeServiceWire20 || accountIdWire20 || authorityEpochLE64 || configurationNonceLE64 || Serialize([target, method, args, nonce, deadline, ByteString.Empty])`.

The digest is SHA256 of that preimage. No varint, signed-Integer serialization, decimal text or platform-endian integer encoding is used for the two fixed 8-byte counters. Offline SDKs require exact vectors and on-chain domain/digest comparison. Verification and Application read committed counters; verification cannot predict a future configuration transition in the transaction.

## Separate counters and plugin storage

`ConfigurationNonce` increments for existing committed configuration/freeze transitions and recovery. It invalidates pending intents and now pending signatures. `AuthorityEpoch` increments only when recovery revokes all modules. Both reject overflow; neither wraps. Storage must not use ConfigurationNonce as its namespace because configuring one child would invalidate every other child's configuration.

All supported ABI 2 native-profile modules namespace every account-scoped persistent key by `(accountId, AuthorityEpoch)`. The safe native getter is the source of the epoch; no caller-provided epoch is accepted. Configuration, signer keys, SessionKey grants, spending counters, hooks, composite topology, and temporary persisted snapshots must use the same helper. Reading, callback authorization, signer-domain discovery and reinstallation must not fall back to prior namespaces. Old bytes may remain unreachable; normal cleanup removes only the current namespace. This module-side change is a required dependency before claiming full recovery closure. Arbitrary malicious plugin honesty remains outside ABI admission guarantees; selecting a newly malicious module is not prevented by a counter.

The native recovery path clears the two fixed dependency keys without deserializing attacker-controlled/plugin-stale records. The service must never grant a detached root/child its old frame context. Existing engine-local leases and transaction rollback continue to constrain reentrancy. No global nonce reset or unbounded iteration over nonce channels is necessary.

## Test-driven validation

Initial real-VM regressions must fail on the old prototype, then pass with ABI 2:

1. Mature recovery detaches a root and enrolled children whose cleanup faults; configuration/dependency/pending state is revoked without touching old module storage.
2. Faulting hook, deleted module and altered pinned code cannot stop recovery. Normal rotation still faults and preserves state.
3. Identity/proxy and nonzero cursors on multiple nonce channels remain unchanged; old custody fails and new custody succeeds using the empty-signature fallback.
4. Frozen status remains; either witness alone cannot unfreeze, joint witnesses can.
5. ABI 2 domain changes independently with authority epoch or configuration nonce; old signed/digest-bound operations fail without nonce consumption; newly signed operations succeed.
6. Unknown queries fault, read-only getter metadata is exact, 13-field records/invalid epoch types/negative and overflowing counters fail closed.
7. Epoch exhaustion fails atomically before detachment or notifications; old pending intents are cleared only on successful execution.
8. Reinstalled supported modules cannot read any old key/session/hook/composite namespace; new configuration succeeds without resurrecting old grants.

Pure encoding tests cover zero and maximum counters with independently generated Python/JS vectors. The existing lifecycle/envelope/context/rollback suite must remain green. Fresh private-chain receipts must match ABI 2 source and artifacts; historical ABI 1 passes are not evidence for this revision.

## Performance and deployment

Recovery work is constant in native storage keys: one account update plus two dependency and two pending-call deletions. No external callback is executed and nonce-channel count does not affect work. The signing domain adds 16 bytes. Module reads add a bounded native epoch query; whole-operation fee receipts, especially multisig composition, must be measured under the configured callback caps. The native profile manifest parameter digest must be regenerated from the agreed canonical ABI 2 parameters before an integrated build.

No original worktree mutation, public activation or deployment is authorized by this document. The convergence gate is independent source review, actual VM failure controls, SDK/module vectors, clean build and disposable private-chain readback. External token allowances or third-party durable authorizations require their own revocation policy and are not erased by native recovery.

## Bounded implementation receipt

On the preserved ABI 1 implementation, five new recovery/domain tests failed for
the intended semantic reasons (joint red log `/tmp/aa-native-combined-red.log`).
After ABI 2 changes, 141 actual VM/state/protocol/module-policy cases passed with
zero failures or skips in `/tmp/aa-native-recovery-epoch-green.log`. The suite
includes deleted/changed module and malformed dependency storage recovery,
independent Python state/domain vectors, and both independent signature-domain
counters. This is local source validation, not public deployment or arbitrary
plugin epoch conformance. Supported-module reinstall isolation is a separate
required integration gate.

## Transaction witnesses also commit both counters

The six-field operation digest protects cryptographic UserOperation verifiers,
but native custody fallback and NeoNativeVerifier authorize through transaction
witnesses. ABI 2 therefore requires all execution transactions to include both
counters as native entrypoint arguments:

- `executeUserOp(accountId, op, expectedAuthorityEpoch, expectedConfigurationNonce)`
- `executeUserOps(accountId, ops, expectedAuthorityEpoch, expectedConfigurationNonce)`

Both parameters are exact VM Integers in the UInt64 domain and must equal the
current account before any authorization, nonce mutation or callback. There is
no two-argument overload or implicit current-counter fallback. Verification
requires the canonical four-argument transaction script and checks the same
counters. Application checks them independently even when no proxy witness is
used. The expected counters are signed transaction bytes; re-exporting an old
signed transaction must never refresh them. A new preview and signature are
required after a configuration or recovery transition.

Identity, six-field operations, operation-digest encoding and every nonce lane
remain unchanged. The native ABI remains version 2 because this is convergence
of an unactivated draft; previous ABI 2 two-argument artifacts are incompatible.
An old raw transaction stays invalid even if custody is later recovered back to
the same key, its VUB remains live, and its operation nonce was never consumed.
