# Dynamic Gas Pricing

This release replaces the legacy fixed fee schedule with a runtime-aware
resource model that charges contracts for the CPU, transient memory, and
persistent storage they actually consume. The change affects every opcode,
syscall, and native contract entrypoint.

## Execution Model

- **ResourceCost** – A new struct that tracks `cpuUnits`, `memoryBytes`, and
  `storageBytes`. `ApplicationEngine` now exposes helpers (`ChargeCpu`,
  `ChargeMemory`, `ChargeStorage`, and `AddResourceCost`) which translate
  resource usage into datoshi using three policy-controlled multipliers:
  `ExecFeeFactor`, `MemoryFeeFactor`, and `StoragePrice`.
- **Memory Fee Factor** – Managed alongside the existing execution and storage
  multipliers by the native `PolicyContract`. The defaults are available even
  on historical state, and governance can tune the factor at runtime through
  the existing policy RPC surface (`PolicyAPI.GetMemoryFeeFactorAsync`).
- **Diagnostic Support** – Per-opcode resource contributions flow through the
  existing `IDiagnostic` hook chain, enabling custom instrumentation and
  trace tooling.

## Opcode Accounting

- `PreExecuteInstruction` now charges the static cost through `ChargeCpu` and
  layers a dynamic adjustment via `CalculateDynamicOpcodeCost`. The estimator
  inspects the evaluation stack to account for operand byte length, element
  counts, and container sizes when executing string, buffer, and collection
  opcodes (`PUSHDATA*`, `CAT`, `SUBSTR`, `LEFT/RIGHT`, `PACK`, `NEWARRAY*`,
  `NEWBUFFER`, `MEMCPY`, etc.).
- Container mutations (`APPEND`, `SETITEM`, `REVERSEITEMS`, map projections)
  add CPU and memory proportional to the element footprint using a small
  heuristic (`AverageElementOverhead`).
- Iterator materialisation (`System.Iterator.Value`) bills the value size while
  preserving immutability on the evaluation stack.
- Numeric, bitwise, and shift opcodes scale with operand size: the estimator
  caps total cost to avoid runaway multiplications while still reflecting the
  expense of big integer arithmetic, modular exponentiation, or wide bitfield
  operations.

## Syscalls & Native Contracts

- Interop descriptors accept optional dynamic calculators. The engine captures
  raw stack arguments before conversion and evaluates any additional resource
  cost prior to dispatching the handler.
- Storage operations meter key lookups, value loads, iterator creation, and
  writes: `System.Storage.Put`/`Delete` now charge CPU per-byte and convert the
  existing storage delta logic to `ChargeStorage`; reads apply CPU + memory
  proportional to the returned value size.
- Storage iterators charge per entry during enumeration, aligning the cost of
  `Iterator.Next` with the key/value data retrieved from the underlying store.
- Runtime facilities (`System.Runtime.Log/Notify/LoadScript/CheckWitness`) are
  instrumented to charge for serialized payloads, script size, and validation
  inputs. Notifications charge per encoded byte at the point of serialization.
- Native contracts leverage the new helpers. Method invocation now maps the
  descriptor metadata (`CpuFee`, `StorageFee`) into resource costs without
  manual datoshi arithmetic. Contract deployment/update charges are expressed
  in terms of storage bytes plus a make-whole component to honour the minimum
  deployment fee.

## Policy & Tooling Updates

- `PolicyContract` persists the `MemoryFeeFactor` alongside existing policy
  knobs. Setter/getter entrypoints and RPC bindings mirror the execution and
  storage controls.
- `PolicyAPI` adds `GetMemoryFeeFactorAsync`, and unit/integration tests cover
  the new RPC method.
- Documentation and CLI examples now reference the memory fee factor and
  describe the conversion pipeline for all three resource classes.

## Migration Guidance

1. **Contract Authors** – Expect gas consumption to scale with payload size.
   Re-run critical flows on testnet and ensure off-chain estimators use the
   updated policy API before mainnet activation. Storage writes remain priced
   per byte; transient allocations and large notifications are now metered.
2. **Node Operators** – Review policy multipliers (`exec`, `memory`,
   `storage`) and adjust to reflect infrastructure costs. The defaults match
   previous behaviour for small payloads and maintain deterministic execution.
3. **Tooling Vendors** – Update SDKs and wallets to query
   `GetMemoryFeeFactorAsync` when modelling gas, and surface richer diagnostics
   where available.

Activation should be coordinated through a dedicated hardfork flag so legacy
state (which lacks the memory fee slot) absorbs the default without manual
migration.
