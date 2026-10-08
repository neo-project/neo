# Bounded contract callbacks

`System.Contract.CallWithGasLimit(contractHash, method, callFlags, gasLimit, args)`
is available only when `HF_SmartAccountV1` is explicitly activated. This is a
resource-control prerequisite, not an implementation of native AccountManagement.

## Resource invariant

The positive `gasLimit`, in datoshi, bounds the cumulative metered work of the
callee and every descendant execution context. Ordinary contract calls, static
calls, local calls, contract initialization, native callbacks, and scripts loaded
by `System.Runtime.LoadScript` must retain the same budget object. A nested
bounded call additionally owns a child budget; charges debit every active ancestor.
Returning from a descendant never resets or refunds its consumed budget.
When instruction pricing occurs after execution, the charge belongs to the
context that executed the instruction, even if that instruction entered a
callee or returned to a caller. In particular, a callback's `RET` remains
subject to that callback's budget.

The syscall's own dispatch and argument-evaluation costs are charged to its
caller. The callee's budget is checked against the remaining transaction and
ancestor budgets after that dispatch cost. Limits are converted to the engine's
internal units with exact integer arithmetic. Budget checks are proportional to
the number of active bounded ancestors, itself bounded by VM invocation limits.

Transaction fee whitelisting does not exempt execution from a callback budget.
Ordinary transaction fee accounting, call permissions, return values, and witness
semantics are otherwise unchanged. In particular, dynamic scripts remain
read-only and do not acquire a deployed contract's identity or additional flags.

Exhaustion faults the engine before the offending charge is applied. It is not a
catchable, successful callback result, nor an isolated transaction. State effects
follow existing transaction FAULT semantics; callers must not assume that they
can continue a successful operation after budget exhaustion.

## Expected behavior

- A bounded GAS `symbol` call with sufficient budget returns `GAS`.
- A deployed self-loop faults with `The bounded contract call gas limit has been
  exhausted.` while transaction gas remains.
- Loading the self-loop through `System.Runtime.LoadScript` has the same result;
  dynamic execution must not escape the parent budget.
- A finite loaded script preserves its return value. After return, later work in
  the original caller is outside the completed callback's budget.
- Nonpositive limits and limits above remaining budgets are rejected.
- Without activation, the new syscall is unavailable and legacy calls retain
  their existing behavior. No public-network activation height is selected here.

## Validation

The focused engine tests exercise both exhaustion paths and legitimate returns.
Private NeoExpress testing must use a node built with the exact core assembly,
activate the hardfork only in the disposable chain, deploy valid NEF fixtures,
and compare deployed script bytes with the fixtures before invoking them.
Do not treat an invalid jump, a missing contract, or failure in the parent's
dispatch cost as evidence that a descendant was bounded.
