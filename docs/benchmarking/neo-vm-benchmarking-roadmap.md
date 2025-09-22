# Neo VM Benchmarking Roadmap

_Last updated: 2025-09-20T14:24:59+08:00_

## 1. Objectives
- Provide repeatable, profiled benchmarks for **all VM opcodes**, **system calls**, and **native contract methods** under varying input complexity.
- Produce structured measurement outputs that can drive a **dynamic gas pricing model** based on observed computational cost.
- Deliver documentation and automation so contributors can reproduce results and extend the suite.

## 2. Scope & Non-Goals
- **In scope:** BenchmarkDotNet-based harnesses, scenario generators, ApplicationEngine-driven syscall/native measurements, data export, CI orchestration.
- **Out of scope (for now):** On-chain deployment utilities, UI visualisations, gas model fitting (handled downstream once data exists).

## 3. Current State Summary
| Area | Status | Notes |
|------|--------|-------|
| Opcode coverage | In-progress | Systematic registry + coverage CSV for opcodes; ensure enforcement remains pending. |
| Syscalls/native | Stubbed or PoC-only | `BenchmarkEngine` fakes syscalls; native contracts unused. |
| Metrics | Structured CSV | Recorder exports per-scenario metrics with complexity-normalised columns. |
| Input scaling | Single hardcoded cases | Lacks micro/standard/stress comparisons. |
| Documentation | Absent | No contributor guidance.

## 4. Deliverables Overview
- Shared benchmarking infrastructure (scenario registry, execution engine extensions, profile definitions).
- Opcode, syscall, and native-contract benchmark suites with parameterised workloads.
- Data export pipeline (CSV/JSON) + summary report generation.
- Developer documentation: usage guide, contribution checklist, troubleshooting.
- CI integration for scheduled runs or manual triggers.

## 5. Work Breakdown Structure

### Phase 0 – Documentation & Alignment *(current phase)*
- [x] Draft roadmap (this document).
- [ ] Sign-off / capture feedback (update doc as decisions land).

### Phase 1 – Foundational Infrastructure
- [x] Scenario profile definitions (micro/standard/stress), reusable scenario interface, benchmark suite base class.
- [x] Enhanced `BenchmarkEngine` hooks (pre/post instruction, gas-limit execution with actual measurement, metrics collection in Release).
- [x] Structured result recorder (in-memory model + CSV/JSON writer).

### Phase 2 – Opcode Coverage
- [ ] Opcode registry mapping → scenario generators per category (push, stack, arithmetic, control, etc.). _Push, stack, arithmetic, bitwise, logic, splice, slot, type, and core control opcodes covered; compound/native interop still pending._
- [ ] Automated stack priming & cleanup to avoid leaks between iterations. _Implemented for handled categories; extend as new ones arrive._
- [ ] BenchmarkDotNet suite enumerating all opcodes with baseline/single/saturated runs.
- [ ] Validation scripts to ensure every opcode has a scenario. _Coverage CSV emitted; integrate enforcement once remaining categories land._

### Phase 3 – Syscall Benchmarking
- [x] ApplicationEngine harness with seeded snapshot & deterministic environment. _(BenchmarkApplicationEngine establishes a per-run MemoryStore snapshot and timing hooks.)_
- [ ] Scenario builder reflecting over `InteropService` entries to auto-generate cases. _(Zero-argument runtime syscalls plus log/notify/burngas/getnotifications/currentSigners/checkWitness/loadScript covered; remaining interops (contract/storage) and richer argument synthesis pending.)_
- [ ] Input scaling (small vs large payloads, varying iterator counts).
- [x] Metrics capture for syscall latency. _(Recorder writes per-scenario syscall/native timings; allocation tracking still TBD.)_

### Phase 4 – Native Contract Benchmarking
- [x] Fixture generating canonical blockchain state per native contract (NativeBenchmarkStateFactory provisions a MemoryStore-backed NeoSystem snapshot)..
- [ ] Scenarios for key entry points (transfer, balance, claim, register, etc.). _(Initial read-only calls for Policy/GAS/NEO/Ledger measured; transactional paths pending.)_.
- [ ] Complex workload variants (batch transfers, large storage payloads).

### Phase 5 – Data Pipeline & Reporting
- [x] Aggregate run output into artefacts (`BenchmarkDotNet` reports + custom summaries). _(CSV summaries emitted for opcode & syscall suites.)_
- [ ] Provide comparison tooling (e.g., Python/PowerShell script) for historical tracking.
- [ ] Define schema for downstream gas modelling consumers.

### Phase 6 – Automation & Quality Gates
- [ ] Add CLI command(s) or scripts to run targeted suites.
- [ ] Integrate with CI (smoke subset by default, full suite on-demand or nightly).
- [ ] Add validation tests (e.g., ensure scenario counts match opcode/syscall inventories).

### Phase 7 – Documentation & Handover
- [ ] Contributor guide covering setup, extending scenarios, interpreting data.
- [ ] Troubleshooting section (common VM setup pitfalls, snapshot regeneration, etc.).
- [ ] Final review & checklist completion.

## 6. Milestone Tracking
| Milestone | Target | Owner | Status |
|-----------|--------|-------|--------|
| Phase 1 foundation | TBC | _assigned later_ | Not started |
| Phase 2 opcode coverage | TBC | _assigned later_ | In progress |
| Phase 3 syscall coverage | TBC | _assigned later_ | In progress |
| Phase 4 native coverage | TBC | _assigned later_ | In progress |
| Phase 5 pipeline | TBC | _assigned later_ | Not started |
| Phase 6 automation | TBC | _assigned later_ | Not started |
| Phase 7 documentation | TBC | _assigned later_ | Not started |

## 7. Open Questions / Decisions Needed
1. **Metric granularity:** Do we need allocation/GC metrics alongside CPU time? (Impacts tooling.)
2. **Data retention:** Where should benchmark artefacts be stored (repo vs external storage)?
3. **CI cadence:** Full suite can be long-running; confirm acceptable schedule.
4. **Snapshot seeding:** Agree on baseline chain state for native-contract runs.
5. **Gas-model integration:** Define interface for downstream modelling scripts (JSON schema).

## 8. Next Steps
1. Gather feedback on this roadmap (update Phase 0 checklist).
2. Kick off Phase 1 tasks (scenario abstractions, engine hooks, metrics recorder).
3. Log progress in this document (update checkboxes, timestamps).

---
_This roadmap should be kept alongside implementation PRs. Update phase sections as tasks complete or requirements evolve._
