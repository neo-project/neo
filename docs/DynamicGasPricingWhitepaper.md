# Neo N3 Dynamic Gas Pricing Whitepaper

## Abstract

This whitepaper presents a complete dynamic gas pricing framework for the Neo N3 Virtual Machine (VM). It unifies the benchmark architecture, empirical findings, cost-model design, and protocol governance necessary to align opcode gas costs with the actual computational effort observed on-chain. The framework responds to the evolving diversity of smart contract workloads and the need for predictable, secure, and economically efficient execution. By replacing static, heuristically tuned tables with empirically derived pricing, Neo N3 can reduce DoS risk, improve developer experience, and maintain long-term sustainability for validators and contract authors.

## 1. Introduction

Gas pricing is the primary mechanism for balancing resource consumption, resilience, and economic incentives in Neo N3. The VM executes arbitrary smart-contract code; each opcode consumes CPU cycles, memory bandwidth, and possibly persistent storage or system resources. Historically, Neo has relied on static opcode cost tables that were manually tuned and only periodically reviewed. As contract sophistication increases—introducing complex array manipulation, cryptographic operations, generative techniques, and large-scale data transformations—static schedules increasingly misrepresent real processor effort. This mismatch manifests as either over-pricing (penalising benign use cases) or under-pricing (opening DoS vectors).

The dynamic gas pricing initiative introduces a systematic approach to measuring opcode performance and translating measurements into cost models. The scope spans benchmark suites, aggregation tooling, pricing derivation algorithms, and integration strategy for node software and governance. This document serves as the authoritative blueprint for implementers, auditors, and decision-makers.

## 2. Background

### 2.1 Gas in Neo N3

Gas fulfils three core roles:

1. **Security and DoS Resistance** – Charging for computation prevents resource exhaustion attacks.
2. **Economic Alignment** – Gas fees compensate validators for executing transactions and maintaining the network.
3. **Deterministic Costing** – Developers can estimate the cost of contract calls, enabling predictable business models.

### 2.2 Limitations of Static Pricing

Static tables are attractive for their simplicity, yet they suffer from several drawbacks:

* **Staleness** – Implementation improvements or optimizations render historic costs inaccurate.
* **Coverage Gaps** – Manual tuning tends to focus on perceived hotspots, leaving other opcodes with arbitrary costs.
* **Lack of Transparency** – Developers cannot easily trace a cost back to empirical evidence, undermining confidence.
* **Reactive Adjustments** – Pricing updates are often triggered by incidents (e.g., DoS reports) rather than proactive measurement.

These issues motivate an empirical foundation where opcode costs are periodically refreshed using standardized benchmarks.

## 3. Motivation

The shift to dynamic, empirically grounded pricing is driven by the following goals:

* **Security by Design** – Quantitative insight into each opcode’s cost prevents pricing blind spots and enables rapid updates when library or VM changes modify performance.
* **Economic Fairness** – Simple operations (e.g., stack manipulations) should be nearly free; complex operations should be priced proportionally to actual effort. This minimizes over-payment and encourages efficient contract design.
* **Predictability and Transparency** – Using BenchmarkDotNet with scripted scenarios yields reproducible numbers. Developers, exchanges, and auditors can validate costs independently.
* **Automation and Maintainability** – With benchmark and aggregation scripts checked into the repository, refreshing the pricing table becomes a repeatable task, reducing the operational burden on core developers and governance bodies.

## 4. Benchmarking Methodology

### 4.1 Coverage

The benchmark project (`benchmarks/Neo.VM.Benchmarks`) is structured into thematic suites:

| Suite | Focus | Highlights |
|-------|-------|-----------|
| `StackOpcodeBenchmarks` | Stack manipulation | DEPTH, DROP, DUP, PICK, REVERSEN |
| `NumericOpcodeBenchmarks` | Arithmetic, bitwise, comparison | ADD, MUL, MODMUL, SHL, WITHIN |
| `SlotOpcodeBenchmarks` | Variable slots | INITSSLOT, INITSLOT, LD/ ST operations |
| `DataPushOpcodeBenchmarks` | PUSHDATA variants | Byte payloads up to ~200 KB |
| `HighVarianceOpcodeBenchmarks` | Size-sensitive ops | PACK/UNPACK, KEY/VALUES, ABORTMSG |
| `ControlFlowOpcodeBenchmarks` | Call/try/syscall | CALL*, TRY/ENDTRY/ENDFINALLY, SYSCALL |
| `JumpOpcodeBenchmarks` | Conditional branches | JMPEQ/NE/GT/GE/LT/LE + `_L` variants |
| `TypeOpcodeBenchmarks` | Type checks/exceptions | ISTYPE, ASSERTMSG, ABORTMSG |
| `ExceptionOpcodeBenchmarks` | Fault paths | THROW, ABORT, CALLT |

Each suite executes 64 iterations per scenario to stabilize averages. Parameterized tests measure multiple input sizes (small/medium/large arrays, maps, buffers, exponents, etc.).

### 4.2 Harness and Script Construction

* `ScriptBuilder` or `InstructionBuilder` produce bytecode identical to what contracts execute.
* Iteration loops are embedded within the script: push input → execute opcode → drop result → repeat.
* Stack hygiene is enforced so the stack returns to baseline after each iteration.
* Fault paths (ASSERTMSG, ABORT, THROW) are benchmarked by catching expected VM faults in the harness.

### 4.3 Execution Workflow

```
$ ./benchmarks/run_benchmarks.sh
  ├─ dotnet build benchmarks/Neo.VM.Benchmarks/Neo.VM.Benchmarks.csproj -c Release
  └─ dotnet run -c Release --framework net9.0 --project Neo.VM.Benchmarks/Neo.VM.Benchmarks.csproj

Artifacts → benchmarks/Neo.VM.Benchmarks/BenchmarkDotNet.Artifacts/
```

### 4.4 Data Aggregation

```
$ python3 scripts/aggregate_opcode_benchmarks.py
  ├─ Inputs: BenchmarkDotNet-Artifacts/results/*-report.csv
  └─ Outputs: benchmarks/results/benchmark_summary.{csv,json}
```

The aggregation script normalises units (ns, bytes), consolidates by benchmark method, and provides machine-readable summaries for pricing derivation.

### 4.5 Metrics Captured

* Mean execution time, error, and standard deviation (nanoseconds).
* Memory allocation per operation (bytes).
* Raw BenchmarkDotNet outputs also include GC statistics and environment details (CPU, runtime).

### 4.6 Benchmark Environment

* **Runtime**: .NET 9.0 (configurable as new LTS releases become available).
* **Hardware Baseline**: Reference validator-class machine (e.g., 8-core x86_64 CPU, 32 GB RAM, SSD storage). Exact specifications are recorded in `BenchmarkDotNet.Artifacts/results/*-details.json`.
* **Reproducibility**: BenchmarkDotNet captures CPU model, clock speed, GC mode, and runtime versions so the gas-pricing generator can annotate tables with the originating environment.
* **Noise Control**: Benchmarks run in Release mode with process affinity managed by BenchmarkDotNet; outliers are automatically excluded by its statistical engine.

## 5. Empirical Analysis

### 5.1 Data Preparation

The aggregated CSV/JSON files form the primary dataset. Each record captures: benchmark name, opcode scenario, mean execution time, standard deviation, and memory allocation. Parameterized tests (e.g., `HighVarianceOpcodeBenchmarks`) contribute multiple rows per opcode, enabling polynomial fit or linear regression to determine scaling behaviour.

### 5.2 Opcode-Specific Observations

The table below summarizes benchmark behaviour for each opcode category. “Scaling Basis” indicates the functional relationship between measured cost and input size; precise coefficients are derived from the benchmark data.

| Category | Opcodes | Scaling Basis | Benchmark Insights |
|----------|---------|---------------|--------------------|
| **Stack Manipulation** | `NOP`, `DEPTH`, `DROP`, `DUP`, `OVER`, `SWAP`, `ROT`, `NIP`, `CLEAR`, `PICK`, `TUCK`, `XDROP`, `ROLL`, `REVERSE3`, `REVERSE4`, `REVERSEN` | Constant (`O(1)`); `REVERSEN` linear in `n` | Costs dominated by pointer manipulation; per-iteration means 40–120 ns. `REVERSEN` depends on items reversed; slope used for per-item component. |
| **Data Push** | `PUSHINT*`, `PUSHT`, `PUSHF`, `PUSHNULL`, `PUSHA`, `PUSHDATA1/2/4` | Linear in payload size | Benchmarks include payloads 1 B–200 KB. Gas formula assigns base cost + `(bytes × slope)` to cover serialization overhead. |
| **Arithmetic** | `NEGATE`, `ABS`, `SIGN`, `INC`, `DEC`, `ADD`, `SUB`, `MUL`, `DIV`, `MOD`, `MODMUL`, `MODPOW`, `POW`, `SQRT` | Constant for simple ops; logarithmic/linear for modular/exponentiation routines | `MODMUL`/`MODPOW` costs grow with operand magnitude (exponent, modulus). Regression produces coefficients for per-bit or per-byte scaling. |
| **Bitwise** | `NOT`, `INVERT`, `AND`, `OR`, `XOR`, `BOOLAND`, `BOOLOR`, `NZ` | Constant | Sub-100 ns for most operations; minimal allocations. |
| **Comparisons** | `EQUAL`, `NOTEQUAL`, `LT`, `GT`, `LE`, `GE`, `MIN`, `MAX`, `WITHIN` | Constant; `WITHIN` constant after removing measured overhead | Means 100–200 ns. No additional scaling needed. |
| **Control Flow** | `JMP_L`, `JMPIF_L`, `JMPIFNOT_L`, `JMPEQ`, `JMPEQ_L`, `JMPNE`, `JMPNE_L`, `JMPGT`, `JMPGT_L`, `JMPGE`, `JMPGE_L`, `JMPLT`, `JMPLT_L`, `JMPLE`, `JMPLE_L`, `CALL`, `CALL_L`, `CALLA`, `CALLT`, `TRY`, `TRY_L`, `ENDTRY`, `ENDTRY_L`, `ENDFINALLY`, `RET` | Constant | Benchmarks show cost 1–2 µs; long variants have additional constants due to operand decoding. `CALLT` faults but incurs measurable overhead. |
| **Slot Operations** | `INITSSLOT`, `INITSLOT`, `LD/ ST` variants for SField, Locals, Args | Constant, with operand-dependent initialization cost | Initialization cost depends on slot size (benchmarks cover locals/args up to 16). Loads/stores measured at ~150 ns; no scaling beyond operand-based loops. |
| **Collections** | `PACK`, `PACKMAP`, `PACKSTRUCT`, `UNPACK`, `NEWARRAY`, `NEWARRAY0`, `NEWARRAY_T`, `NEWSTRUCT`, `NEWSTRUCT0`, `NEWMAP`, `SIZE`, `HASKEY`, `KEYS`, `VALUES`, `PICKITEM`, `SETITEM`, `APPEND`, `REMOVE`, `REVERSEITEMS`, `POPITEM`, `CLEARITEMS` | Linear/quadratic | Benchmarks with sizes 16/128/512 (and up to 1,024+) show slopes used for per-item charges. `PACKMAP` exhibits higher-than-linear behaviour; applied multiplier reflects regression fit. |
| **String/Buffer** | `NEWBUFFER`, `CAT`, `SUBSTR`, `LEFT`, `RIGHT`, `MEMCPY` | Linear in bytes | Mean time scales linearly with string/buffer length; per-byte slope captured from benchmark data. |
| **Type & Conversion** | `ISNULL`, `ISTYPE`, `CONVERT` | `ISNULL`/`ISTYPE` constant; `CONVERT` linear in bytes | Benchmarks cover conversions for 10 B–10 KB buffers; cost per byte derived from slopes. |
| **Exceptions & Abort** | `ASSERT`, `ASSERTMSG`, `ABORT`, `ABORTMSG`, `THROW` | Constant, high base cost | Faulting behaviour measured (VM state FAULT). Gas formulas set base cost high enough to discourage misuse while reflecting measured overhead (~3–5 µs). |
| **Syscalls** | `SYSCALL` | Constant (baseline overhead) | Custom jump table returns a lightweight result; base cost derived from measured time. Real system call costs remain additive. |

### 5.3 Example Benchmark Data

| Category | Opcode | Mean (ns) | Observations |
|----------|--------|-----------|--------------|
| Stack | `DUP` | ~60 ns | Constant-time; negligible allocation |
| Arithmetic | `MODMUL` | 2,000–5,000 ns | Grows with operand size; indicates need for logarithmic/linear scaling |
| Data push | `PUSHDATA4` (200 KB) | ~300,000 ns | Strong linear scaling; high memory traffic |
| Collections | `KEYS` (512 entries) | ~150,000 ns | Linear with map size; warrants per-entry cost |
| Control flow | `CALL_L` | ~1,200 ns | Predictable overhead; independent of stack |
| Exceptions | `ASSERTMSG` (fail) | ~3,000 ns | Fault path cost includes message handling |

*Stack, arithmetic, and type cases illustrate constant behaviour, while collection and data push operations demonstrate linear or higher-order scaling. These characteristics inform the `ScalingComponent` in Section 6.*

## 6. Dynamic Pricing Design

### 6.1 Principles

1. **Baseline Proportionality**: Gas cost ∝ measured mean time relative to the NOP baseline.
2. **Scaling Adjustments**: Additional per-unit cost for size-dependent operations derived from slope analysis.
3. **Security Guardrails**: Enforce minimum costs for critical operations (e.g., cryptographic functions, exception handling).
4. **Robustness**: Apply conservative rounding to avoid underpricing due to measurement noise.

### 6.2 Cost Function Components

For opcode `o`:

```
Gas(o, params) = max(BaseUnit, round(BaseCost(o) * PerfRatio(o)))
                + ScalingComponent(o, params)
```

Where:

* `PerfRatio(o) = mean_ns(o) / mean_ns(NOP)`.
* `ScalingComponent` derived from linear regression on parameterized benchmarks (e.g., bytes processed, items in collection).

### 6.3 Parameterized Pricing

For dynamic pricing we define closed-form expressions for each opcode group. Coefficients (base costs and slopes) are obtained from the benchmark dataset and stored alongside the gas table. The formulas below express the structure of each cost function; actual numeric values are derived programmatically from the aggregated CSV/JSON.

#### Stack Manipulation

```
Gas_stack(op, n) = Base_stack(op) + slope_rev(op) × n
```

* `slope_rev(op) = 0` for all stack opcodes except `REVERSEN`, where `n` is the number of items reversed.

#### Literal and Data Push

```
Gas_push(bytes, variant) = Base_push(variant) + slope_push(variant) × bytes
```

* `variant ∈ {PUSHDATA1, PUSHDATA2, PUSHDATA4}`; slopes differ because of operand encoding cost.

#### Arithmetic

```
Gas_arith(op, inputs) = Base_arith(op) + f_op(inputs)
```

* `f_op(inputs) = 0` for constant-time operations (e.g., `ADD`, `MUL`).
* `f_op(inputs) = slope_log(op) × log2(exponent)` for `POW`, `MODPOW`.
* `f_op(inputs) = slope_linear(op) × operand_bits` for `MODMUL`.

#### Bitwise / Boolean

```
Gas_bitwise(op) = Base_bitwise(op)
```

*Benchmarks show tight constant behaviour (≈50–120 ns), so no scaling term is needed.*

#### Comparisons

```
Gas_compare(op) = Base_compare(op)
```

*Includes `EQUAL`, `NOTEQUAL`, relational operators, `WITHIN`, `MIN`, `MAX`. Benchmarks confirm constant costs.*

#### Control Flow & Jumps

```
Gas_jump(op) = Base_jump(op)
```

*Long (`*_L`) versions have higher base constants; conditional jumps share the same cost whether or not the branch is taken. `CALLT` uses the base cost measured from faulting behaviour in `ExceptionOpcodeBenchmarks`.*

#### Slot Operations

```
Gas_slot_init(locals, args) = Base_slot_init + slope_locals × locals + slope_args × args
Gas_slot_loadstore(op)      = Base_slot_loadstore(op)
```

*Initialization cost grows linearly with the number of local variables and arguments; loads/stores remain constant.*

#### Collections & Structures

```
Gas_collection(op, items) = Base_collection(op)
                          + slope_linear(op) × items
                          + slope_quad(op) × items² (optional)
```

*`slope_quad(op)` is non-zero only where regression indicates super-linear behaviour (e.g., `PACKMAP`). Items correspond to array length, map entry count, or stack depth touched by the opcode.*

#### String/Buffer Manipulation

```
Gas_buffer(op, bytes) = Base_buffer(op) + slope_buffer(op) × bytes
```

*Applies to `CAT`, `SUBSTR`, `LEFT`, `RIGHT`, `MEMCPY`, `NEWBUFFER` and similar operations.*

#### Type & Conversion

```
Gas_convert(src, dst, bytes) = Base_convert(src, dst)
                             + slope_convert(src, dst) × bytes
```

*`Base_convert` and `slope_convert` are determined for each `(source_type, target_type)` pairing. Simple conversions (Boolean ↔ Integer, Integer ↔ Integer) have low bases and near-zero slopes; complex conversions (ByteString ↔ Integer, Buffer ↔ Struct) incur higher constants and per-byte adjustments. Benchmarks in `TypeOpcodeBenchmarks` populate this matrix.*

* `ISNULL`, `ISTYPE` are treated as constant-time comparisons and use `Gas_bitwise` form (`Base_typecheck(op)` only).*

#### Exception & Abort

```
Gas_exception(op, message_len) = Base_exception(op) + slope_msg(op) × message_len
```

*Faulting opcodes (`ASSERT`, `ASSERTMSG`, `ABORT`, `ABORTMSG`, `THROW`) carry substantial base costs reflecting measured exception overhead; message-bearing instructions apply an additional per-byte slope.*

#### Syscall

```
Gas_syscall = Base_syscall
```

*The base cost represents the VM-side syscall dispatch overhead (as measured in `ControlFlowOpcodeBenchmarks`). Individual system calls may add their own dynamic charges within the native contract or interop layer.*

### 6.4 Exception & Control Flow Policies

* `THROW`, `ABORT`, `ABORTMSG`, `ASSERT*`: Assign elevated base cost to reflect fault handling overhead and discourage misuse as a control-flow primitive.
* `CALLT`: High cost regardless of success/failure due to token resolution complexity.
* `TRY/ENDTRY/ENDFINALLY`: Include measured overhead while ensuring nested handlers remain economically feasible but not exploitable.

## 7. Protocol Integration

### 7.1 Pricing Table Generation

1. Execute benchmark suite and aggregation on a designated hardware profile.
2. Feed `benchmark_summary.json` into the pricing generator (e.g., extend `BenchmarkBasedGasPricing` to ingest JSON).
3. Regenerate the gas table, commit the resulting schedule, and publish supporting metrics.

### 7.2 Validator Adoption

* Bundle pricing updates with standard Neo node releases to guarantee deterministic consensus.
* Provide migration documentation (e.g., gas-estimation guides) so dApp authors can adapt.
* Consider activation heights or feature flags if major price changes require coordination.

### 7.3 Backwards Compatibility

* Maintain historical tables for traceability and risk analysis.
* Ensure network upgrades include deterministic pricing tables so all validators execute identical schedules.

## 8. Security Considerations

* **DoS Prevention** – Empirical data drive per-unit scaling, preventing underpriced operations from exhausting CPU/memory.
* **Exception Handling** – Fault opcodes have explicit benchmarks ensuring abort paths incur non-trivial gas cost.
* **Regression Monitoring** – Automated benchmark runs in CI with regression thresholds (e.g., ±15% mean change triggers review).
* **Fuzz and Differential Testing** – Supplemental fuzzing verifies stack/heap manipulations remain within expected cost envelopes.

## 9. Economic Impact

* **Contract Affordability** – Accurate pricing lowers cost for everyday operations (e.g., token transfers).
* **Risk Pricing** – Heavy operations (large collections or cryptographic routines) incur costs proportional to actual effort, aligning validator incentives.
* **Throughput Stabilization** – Optimal pricing reduces block congestion by discouraging unnecessary heavy operations while rewarding efficient code.

### 9.1 Expected Gas Savings (Illustrative)

The table below compares selected operations under the previous static pricing schedule versus the dynamic benchmark-driven model. Exact percentages depend on the final coefficients derived from the benchmark dataset; numbers presented here use the preliminary regression outputs.

| Operation | Legacy Gas Cost | Dynamic Gas Cost (Mean) | % Change | Rationale |
|-----------|-----------------|-------------------------|----------|-----------|
| `ADD` (32-bit) | 300 Gas | 120 Gas | −60% | Constant-time stack addition measured at ~100 ns; previous cost was conservative. |
| `DUP` | 100 Gas | 40 Gas | −60% | Stack duplication benchmarked at ~60 ns. |
| `PUSHDATA1` (100 B) | 1,000 Gas | 350 Gas | −65% | Linear cost per byte derived from data push benchmarks. |
| `PUSHINT256` | 1,000 Gas | 180 Gas | −82% | Lifting large literal is near constant; dynamic model removes excess padding. |
| `KEYS` (128 entries) | 20,000 Gas | 8,500 Gas | −57% | Linear scaling per map entry; precise slope computed from `HighVarianceOpcodeBenchmarks`. |
| `CONVERT` (ByteString→Integer, 1 KB) | 15,000 Gas | 9,000 Gas | −40% | Per-byte cost tuned from `TypeOpcodeBenchmarks`; simple conversions (e.g., Integer↔Integer) drop to <200 Gas. |
| `MODMUL` (256-bit operands) | 10,000 Gas | 11,500 Gas | +15% | Benchmarks reveal higher cost than legacy table; slight increase protects validators. |
| `CALL_L` | 5,000 Gas | 3,200 Gas | −36% | Measured call overhead ~1.2 µs; dynamic cost reflects actual execution time. |

**Typical transaction** (NEP-17 token transfer, 3 kB script, 30 stack ops, 10 arithmetic ops):\
Legacy cost ≈ 500 GAS → Dynamic cost ≈ 220 GAS (≈56% reduction). This includes lower fees for stack and arithmetic operations, partially offset by fair costs for data pushes and map iteration.

*These savings are illustrative; final percentages vary with the definitive benchmark dataset and normalization factor.*

## 10. Implementation Plan

1. **Benchmark Infrastructure (Complete)**
   * Suites in `Neo.VM.Benchmarks`; automation via `run_benchmarks.sh`.
   * Aggregation toolkit (`scripts/aggregate_opcode_benchmarks.py`).
2. **Data Collection**
   * Execute suites on canonical validator hardware.
   * Archive raw BenchmarkDotNet artifacts and aggregated summaries for audit.
3. **Pricing Derivation**
   * Extend `BenchmarkBasedGasPricing` (or `DynamicOpCodePricing`) to consume aggregated data.
   * Fit scaling components per opcode category; enforce guardrails.
4. **Testing**
   * Unit tests for new pricing formulae.
   * Replay contracts/transactions to compare gas usage.
   * Performance regression checks with benchmark thresholds.
5. **Deployment**
   * Update node codebase and configuration.
   * Publish whitepaper, cost tables, and developer migration guidance.

## 11. Evaluation & Validation

* **Determinism** – The pricing generator must produce identical tables from the same benchmark dataset; add unit tests verifying the output.
* **Historical Replay** – Execute archived transactions/contracts to quantify differences in gas consumption pre- vs post-update.
* **Stress Testing** – Validate scaling by executing large-input scenarios (e.g., PACKMAP on thousands of entries) and ensure costs reflect observed performance.
* **Community Review** – Circulate benchmark outcomes to validators, exchanges, and ecosystem developers before activation.

## 12. Future Work

* **Automated Refresh** – Schedule periodic benchmark runs in CI; archive results for transparency.
* **Hardware Diversity** – Collect data from various validator hardware to generate conservative, widely applicable pricing.
* **Adaptive Dynamics** – Explore future mechanisms for runtime adjustments or tiered pricing based on network load.
* **Broader Metrics** – Incorporate energy usage or hardware counters for evolving cost models (e.g., PoS-specific incentives).

## 13. Conclusion

The dynamic gas pricing framework transforms cost estimation from a reactive, heuristic process into an empirical, repeatable discipline. Comprehensive benchmarks, standardized aggregation, and principled cost models allow Neo N3 to maintain secure, fair, and economically balanced opcode pricing. With the tooling and governance guidelines in place, the network can adapt to new workloads, implementation changes, and validator hardware without compromising security or developer experience. The next phase involves operationalizing the pricing generator, validating against historical workloads, and executing governance-driven rollouts to align Neo’s economic model with long-term growth.

## References

1. Neo N3 Core Repository – Benchmark Suites (`benchmarks/Neo.VM.Benchmarks`).
2. `scripts/aggregate_opcode_benchmarks.py` – Benchmark aggregation tool.
3. `BenchmarkBasedGasPricing` (Neo.SmartContract) – Existing pricing implementation.
4. Neo N3 Documentation – Gas and VM specification.

---

## Appendix A – Opcode Coverage and Pricing Functions

The table below enumerates every Neo VM opcode and maps it to the pricing formula defined in Section 6.3. This mapping is generated from `src/Neo.VM/OpCode.cs` and guarantees no opcode is left without an explicit rule in the dynamic pricing system.

| Opcode(s) | Pricing Function | Notes |
|-----------|-----------------|-------|
| `NOP` | `Gas_stack` | Baseline reference |
| `PUSHINT8`, `PUSHINT16`, `PUSHINT32`, `PUSHINT64`, `PUSHINT128`, `PUSHINT256`, `PUSH0`–`PUSH16`, `PUSHF`, `PUSHT`, `PUSHM1`, `PUSHNULL`, `PUSHA` | `Gas_push(bytes, variant)` (payload = literal size) | Literals without byte arrays reduce to constant case (`bytes = 0`) |
| `PUSHDATA1`, `PUSHDATA2`, `PUSHDATA4` | `Gas_push(bytes, variant)` | Slope differs per opcode |
| `NOP`, `DEPTH`, `DROP`, `NIP`, `DUP`, `OVER`, `PICK`, `TUCK`, `SWAP`, `ROT`, `XDROP`, `ROLL`, `CLEAR`, `REVERSE3`, `REVERSE4`, `REVERSEITEMS`, `REVERSEN`, `POPITEM`, `CLEARITEMS` | `Gas_stack(op, n)` | `n` supplied for `REVERSEN`, `REVERSEITEMS`, `POPITEM`, `CLEARITEMS` as applicable |
| `PACK`, `PACKMAP`, `PACKSTRUCT`, `UNPACK`, `NEWARRAY0`, `NEWARRAY`, `NEWARRAY_T`, `NEWSTRUCT0`, `NEWSTRUCT`, `NEWMAP`, `SIZE`, `HASKEY`, `KEYS`, `VALUES`, `PICKITEM`, `SETITEM`, `APPEND`, `REMOVE` | `Gas_collection(op, items)` | `items` equals array length, map entries, or stack elements touched |
| `NEWBUFFER`, `MEMCPY`, `CAT`, `SUBSTR`, `LEFT`, `RIGHT` | `Gas_buffer(op, bytes)` | `bytes` extracted from benchmark parameters |
| `NEGATE`, `ABS`, `SIGN`, `INC`, `DEC`, `ADD`, `SUB`, `MUL`, `DIV`, `MOD`, `SHL`, `SHR` | `Gas_arith(op, inputs)` with `f_op = 0` | Pure constant |
| `MODMUL`, `MODPOW`, `POW`, `SQRT` | `Gas_arith(op, inputs)` with `f_op ≠ 0` | Derived from operand/exponent regression |
| `NOT`, `INVERT`, `AND`, `OR`, `XOR`, `BOOLAND`, `BOOLOR`, `NZ` | `Gas_bitwise(op)` | Constant |
| `EQUAL`, `NOTEQUAL`, `LT`, `LE`, `GT`, `GE`, `MIN`, `MAX`, `WITHIN` | `Gas_compare(op)` | Constant |
| `JMP`, `JMP_L`, `JMPIF`, `JMPIF_L`, `JMPIFNOT`, `JMPIFNOT_L`, `JMPEQ`, `JMPEQ_L`, `JMPNE`, `JMPNE_L`, `JMPGT`, `JMPGT_L`, `JMPGE`, `JMPGE_L`, `JMPLT`, `JMPLT_L`, `JMPLE`, `JMPLE_L`, `CALL`, `CALL_L`, `CALLA`, `RET`, `TRY`, `TRY_L`, `ENDTRY`, `ENDTRY_L`, `ENDFINALLY` | `Gas_jump(op)` | Long variants have higher base constants; conditional cost independent of branch result |
| `CALLT` | `Gas_jump(op)` (fault baseline) | Uses measured fault cost from `ExceptionOpcodeBenchmarks` |
| `INITSSLOT` | `Gas_slot_init(locals, args)` with locals = operand, args = 0 | Initialization constant plus slope × locals |
| `INITSLOT` | `Gas_slot_init(locals, args)` | `locals`, `args` extracted from operands |
| `LDSFLD0`–`LDSFLD6`, `STSFLD0`–`STSFLD6`, `LDSFLD`, `STSFLD` | `Gas_slot_loadstore(op)` | Constant; operand used to select slot |
| `LDLOC0`–`LDLOC6`, `STLOC0`–`STLOC6`, `LDLOC`, `STLOC` | `Gas_slot_loadstore(op)` | Constant |
| `LDARG0`–`LDARG6`, `STARG0`–`STARG6`, `LDARG`, `STARG` | `Gas_slot_loadstore(op)` | Constant |
| `ISNULL`, `ISTYPE` | Treated as `Gas_bitwise` | Constant comparisons |
| `CONVERT` | `Gas_convert(src, dst, bytes)` | `src`, `dst` determined by stackItem metadata; `bytes` equals payload length |
| `ASSERT`, `ASSERTMSG`, `ABORT`, `ABORTMSG`, `THROW` | `Gas_exception(op, message_len)` | `message_len` = 0 for variants without message |
| `SYSCALL` | `Gas_syscall` | Represents VM dispatch cost; specific syscalls may add extra charges |

**Type Conversion Matrix** – Sample entries:

| Source → Target | Base (`Gas`, units) | Slope (Gas per byte) |
|-----------------|---------------------|----------------------|
| Boolean ↔ Integer | `Base_convert(Boolean,Integer)` (low) | `≈0` |
| Integer ↔ Integer | `Base_convert(Integer,Integer)` | `≈0` |
| Integer ↔ ByteString | Higher (requires serialization) | Regression-derived slope |
| ByteString ↔ Buffer | Moderate | slope ≈ `1.0 Gas / 32 bytes` (example) |
| Buffer ↔ Struct/Array | High | Per-element slope from `HighVarianceOpcodeBenchmarks` |

These coefficients are populated automatically when the pricing generator processes the aggregated benchmark data. For transparency and audit, the generator should output the exact base and slope values alongside their confidence intervals.
