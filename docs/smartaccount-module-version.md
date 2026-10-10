# Native module version admission

AccountManagement ABI 2 changes recovery from custody rotation to complete
authorization revocation. A module that stores account configuration must use
the current `getAuthorityEpoch(accountId)` in its storage namespace. Reinstalling
an old module must not restore pre-recovery keys or policy state.

Before lifecycle discovery or callbacks, admission requires the exact numeric
manifest declaration `extra.smartAccount.abiVersion = 2`, exact lowercase
`extra.smartAccount.profileDigest` equal to the service parameter digest, and
an exact Boolean `extra.smartAccount.compositeVerifier`. Hooks set the Boolean
false; verifier discovery must agree with it. Composite verifier admission also
requires both new exact receipt callback signatures. These requirements reject
older ABI-2 artifacts even when the numeric ABI version matches. Missing declarations,
ABI 1, strings, booleans and other versions are rejected. The declaration is
included in the existing NEF plus canonical-manifest code identity, so changing
it invalidates installed bindings. This is a compatibility gate, not a proof
that arbitrary third-party bytecode implements the declared storage semantics.
Module authors and account operators must review epoch isolation; the supported
module implementations are tested through recovery and reinstallation.

Safe epoch reads do not need an additional privileged module grant. Configuration,
cleanup and execution continue to require the existing exact phase and frame
authorization. This gate does not grant any additional witness authority.

There is no public ABI 1 deployment migration: these changes concern the inactive
native draft. Rebuild modules and register fresh accounts on a new private chain
when upgrading a prototype. Legacy deployed V3 modules remain a separate profile.
