# Design Plan: BFTRAND-style Low-Latency Random Number Provider for Neo N3

**Status:** Design draft (not implemented)  
**Type:** Consensus + NeoVM / native API  
**Credits & sources:**
- Research & paper: [**@Jim8y** (Jimmy)](https://github.com/Jim8y) — *BFTRAND: Low-latency Random Number Provider for BFT Smart Contracts* (DSN 2024, paper389)
- Implementation base in the paper: Neo **dBFT** + **NeoVM**
- Related Neo discussion draft: randomness / RNP tracking for `neo-project/neo`

---

## 1. Motivation

### 1.1 Why Neo needs this

Neo smart contracts are deterministic. Many dApps still need **secure randomness** (NFT rarity, games, lotteries, fair distribution, sampling). Today Neo exposes:

```text
System.Runtime.GetRandom  →  ApplicationEngine.GetRandom()
```

Current behavior (post–`HF_Aspidochelone`):

- Seed from transaction hash bytes (+ block nonce mix-in)
- Iterate with Murmur128 and a per-engine counter
- **Not** backed by a distributed random beacon
- Does **not** meet a formal RNP threat model (unpredictability against consensus-adjacent adversaries, uniqueness domain separation, irreversibility against post-reveal undo)

Industry “secure” RNPs (Chainlink VRF, DRAND, etc.) typically use **commit–execute**:

```text
Round i:     T_commit   (lock request)
Round i+1:   T_execute  (consume revealed random)
```

That costs **two consensus rounds**, **two fee-paying transactions**, and extra on-chain data.

### 1.2 What BFTRAND proposes

BFTRAND integrates a **Distributed Random Beacon (DRB)** into **BFT consensus** so that:

1. Each consensus round produces a **beacon** `σ` for the block.
2. Smart contracts obtain randomness **during the same round** as the requesting transaction (no second tx).
3. Security targets: **pseudorandomness**, **uniqueness**, **availability**, **irreversibility**.
4. Explicit mitigation of **Post-reveal Undo Attacks (PUAs)**, including **Script PUA** (highly relevant to NeoVM).

Paper prototype used **Neo dBFT + NeoVM (C#)** and reported large fee/storage wins vs commit–execute RNPs with negligible consensus overhead at 15s blocks.

### 1.3 Credits

| Credit | Role |
|--------|------|
| **[@Jim8y](https://github.com/Jim8y)** | Author / research lead for BFTRAND; paper and prototype direction that this design follows |
| DSN 2024 paper389 | Formal problem statement, PUA taxonomy, protocol sketch, evaluation numbers |
| Neo Project | dBFT, NeoVM, existing `GetRandom` surface this plan extends |

This design plan is an **engineering adaptation** of BFTRAND to Neo N3’s current architecture (`neo-project/neo`), not a line-by-line port of any private prototype.

---

## 2. Goals and non-goals

### 2.1 Goals

1. **Single-round randomness** for contracts: request + use in one application execution when possible.
2. **Consensus-backed entropy**: beacon not solely derived from attacker-controlled tx fields.
3. **Deterministic replay**: all honest nodes derive the same `GetRandom` stream for a given block + tx context.
4. **View-change safety**: beacons must not leak across dBFT views (bind round id to `(height, view)`).
5. **PUA awareness**: document and optionally enforce irreversibility / IVD-style checks.
6. **HF-gated rollout** with clear fallback to today’s Murmur-based `GetRandom` pre-fork.
7. **Bounded cost**: picoGAS pricing; limited per-block beacon storage.

### 2.2 Non-goals (v1)

- Replacing oracle HTTPS randomness (different trust model).
- Hardware TEE RNPs (Automata-style).
- Full formal verification in-tree (paper proofs stay external; Neo ships engineering invariants + tests).
- Perfect unbiasability against a corrupt **primary** without DRB threshold (v1 still assumes standard dBFT `f < n/3`).

---

## 3. Threat model (Neo-specific)

Adversary **A** controls up to **f** consensus nodes, `f < n/3` (mainnet n=7 → f=2).

| Attack | Description | BFTRAND / Neo response |
|--------|-------------|-------------------------|
| **Precomputation** | Predict future random before inclusion | Beacon finalized only with threshold partials; PRF domain includes tx hash |
| **Bias / grinding** | Choose txs to bias outcomes | Beacon independent of single tx content; PRF keyed by beacon |
| **Replay / collision** | Same random for multiple calls | Counter + tx hash in PRF |
| **View leakage** | Reuse beacon across views | DRB round id = `H(height ‖ view)` or Cantor pairing `(b, v)` |
| **Contract PUA** | Call victim, revert if rarity bad | App-level commit patterns + optional IVD policy |
| **Fallback PUA** | Abuse payment/fallback callbacks | Optional IVD; contract guidelines |
| **Fee PUA** | Fee only covers “good” branch | Optional path fee coverage checks |
| **Script PUA** | Tx verification script aborts after inspecting random | Neo-specific; verification vs application separation + optional policy |

---

## 4. Architecture overview

```text
┌─────────────────────────────────────────────────────────────┐
│ dBFT consensus (speakers / delegates)                       │
│  Prepare / Response / Commit                                 │
│       │                                                      │
│       ├─ partial beacon σ_i + proof π_i  (DRB.Partial)       │
│       └─ aggregate σ  (DRB.Comb) once ≥ k partials           │
│              │                                               │
│              ▼                                               │
│  Block carries BeaconPayload (or native store at OnPersist) │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ ApplicationEngine                                            │
│  nonceData seed  :=  beacon ‖ network ‖ txHash[0:16] …       │
│  GetRandom()     :=  PRF(beacon, txHash, callIndex++)        │
│  Optional: Runtime.GetBlockBeacon() for advanced contracts   │
└─────────────────────────────────────────────────────────────┘
```

### 4.1 Components

| Component | Location (planned) | Responsibility |
|-----------|-------------------|----------------|
| **DRB crypto** | `Neo.Cryptography` (BLS12-381 already in tree) | Partial / combine / verify |
| **Consensus plugin** | `neo-modules` / consensus (out of this repo or plugin) | Exchange partials; attach beacon to block |
| **Block / header field or attribute** | `Neo` payloads | Commit beacon for light clients & replay |
| **Native `Random` or extend Runtime** | `ApplicationEngine` / optional native | API + pricing |
| **Policy params** | `PolicyContract` | k, max GetRandom per tx, fees |
| **PUA / IVD (optional phase)** | verification / plugin | Script & fee path checks |

---

## 5. Protocol design (engineering adaptation)

### 5.1 DRB parameters (mainnet-oriented defaults)

Assume dBFT validators `n = ValidatorsCount` (7), Byzantine bound `t = ⌊(n−1)/3⌋` (2).

| Parameter | Suggested v1 | Notes |
|-----------|--------------|--------|
| Threshold `k` | `t+1` … `2t+1` (paper: `(t, 2t+1]`) | Prefer `k = 2t+1 = 5` for mainnet safety, or `k = t+1 = 3` for liveness under load — **TC decision** |
| Round id `rn` | `Hash(network ‖ height ‖ view)` | Prevents view-change beacon reuse |
| Signature scheme | BLS threshold (align with existing BLS deps) | Matches paper BLS DRB line |
| Beacon size | 32–48 bytes compressed | Fixed field preferred |

**Paper insight (must implement):** Do **not** set DRB round = height alone. When dBFT view-changes, the same height with a new view must use a **new** `rn`, otherwise an attacker who saw a pre-commit beacon can precompute for the new view.

### 5.2 Consensus message flow (dBFT mapping)

| dBFT phase | BFTRAND action |
|------------|----------------|
| **Prepare (speaker)** | Include `rn`; start/collect partials; may attach speaker partial |
| **PrepareResponse** | Each CN attaches `(σ_i, π_i)` for `rn` |
| **Commit** | After `≥ k` valid partials, `σ = Comb(...)`; all honest nodes agree on `σ` |
| **Block persist** | Store `σ` in block body extension / header / native |
| **Recovery / ChangeView** | New view → new `rn`; discard partials for old view |

**Timeout / missing partials:** If `k` partials not available by Commit deadline, either:

- **A (strict):** hold block / view-change (liveness cost), or  
- **B (fallback):** block without beacon; `GetRandom` falls back to pre-HF algorithm for that block only (security downgrade — document clearly), or  
- **C (hybrid):** use previous block beacon + height (weaker; not recommended for v1)

**Recommendation:** Prefer **A** with tuned timeouts for mainnet; **B** only for private nets via Policy flag.

### 5.3 Deriving contract randomness (PRF)

Replace / hardfork `GetRandom` after beacon activation:

```text
// Conceptual
seed0   = Beacon ‖ NetworkMagic ‖ TxHash
r_i     = Murmur128_or_SHA256_PRF(seed0, i)   // i = randomTimes++
return  BigInteger(r_i, unsigned)
```

**Domain separation requirements:**

1. **Per transaction:** include full `Transaction.Hash` (not only 16 bytes if feasible).  
2. **Per call:** monotonic `randomTimes` within engine (already present).  
3. **Per network:** include `ProtocolSettings.Network`.  
4. **Optional per contract:** `CallingScriptHash` if TC wants contract-scoped streams.

**Uniqueness:** Same as paper: deterministic PRF with unique inputs ⇒ unique outputs for honest engines.

### 5.4 Block commitment format (options)

| Option | Pros | Cons |
|--------|------|------|
| **New header field** | Clean light-client verification | Wire break; HF required |
| **Extensible payload / attribute** | Flexible | Discoverability |
| **Native contract storage on OnPersist** | No header change | Extra state; harder for light clients |
| **Witness-adjacent structure** | Near existing multi-sig | Messy |

**Recommendation:** HF-gated **optional header extension** or fixed-size field next to consensus metadata; mirror into native for contracts via `Ledger` / `Runtime.GetBlockBeacon(index)`.

---

## 6. Neo API design

### 6.1 Interop (minimal migration)

Keep:

```text
System.Runtime.GetRandom → BigInteger
```

**Pre-HF_Rnp (name TBD):** existing Murmur path.  
**Post-HF_Rnp:** beacon-based PRF path; FAULT or deterministic fallback if beacon missing (Policy).

### 6.2 Optional new interops / native methods

| API | Purpose |
|-----|---------|
| `System.Runtime.GetBlockBeacon()` | Return current block beacon bytes (advanced) |
| `System.Runtime.GetRandom(uint salt)` | Explicit salt for multi-stream contracts |
| Native `Random.getBeacon(uint index)` | Historical beacon query (if stored) |

### 6.3 Pricing (Policy-tunable)

| Call | Suggested fee | Rationale |
|------|---------------|-----------|
| `GetRandom` | ≥ current post-Aspidochelone (`1<<13` × exec factor) | Keep spam resistance |
| Beacon verify (node-side) | Off-chain / consensus | Not application fee |
| Optional IVD checks | Verification path cost | If enabled |

### 6.4 Compatibility

- Contracts that already call `GetRandom` keep compiling.  
- Semantic change is **HF-visible** (must be in hardfork notes).  
- Document that post-HF randomness is **not** bit-compatible with pre-HF streams.

---

## 7. Post-reveal Undo Attacks (PUA) plan

### 7.1 Taxonomy → Neo mapping

| PUA | Neo example | v1 response |
|-----|-------------|-------------|
| Contract | Malicious contract calls NFT mint, reverts if not rare | Docs + patterns (commit–reveal in app); optional runtime limits |
| Fallback | NEP-17 callback inspects state and reverts | Contract guidelines |
| Fee | High fee only on rare branch | Optional fee-path analysis |
| **Script** | Verification script / dynamic call aborts after random | Strongest Neo-specific risk |

### 7.2 Irreversibility strategies (phased)

**Phase R0 — Documentation only**  
Publish “secure randomness patterns” for Neo contracts (don’t branch-and-revert on random).

**Phase R1 — IVD lite (optional Policy flag)**  
At verification time, reject txs whose script structure matches known Script-PUA patterns (e.g. call GetRandom then conditional ABORT in verification context). Scope carefully to avoid false positives.

**Phase R2 — Full IVD (paper-style)**  
Fee coverage of all paths, script size bounds, entry vs verification script checks — research → NEP.

**Recommendation:** Ship **R0 + R1 research** with consensus RNP; do not block RNP on full IVD.

---

## 8. Phased delivery plan

### Phase 0 — Spec & threat model (2–4 weeks)

**Deliverables:**

- NEP draft: “Neo Runtime Randomness / Block Beacon”
- Threat model + property definitions (pseudorandomness, uniqueness, availability, irreversibility)
- Credit @Jim8y / BFTRAND paper in NEP references
- Decision record: k, header vs native storage, fallback policy

**Exit:** TC review of NEP outline.

### Phase 1 — Crypto primitives (parallel)

**In `neo` / cryptography packages:**

- Threshold BLS partial / combine / verify APIs suitable for validators
- Unit tests: honest combine, invalid partial rejection, deterministic combine order
- Benchmarks for n=7 and n=21

**Exit:** Pure crypto library + tests; no consensus change yet.

### Phase 2 — Consensus integration (neo-modules / consensus plugin)

**Work:**

- Extend consensus messages with optional `BeaconPartial`
- Speaker/delegate state machine: collect ≥ k, compute `σ`
- Bind `rn = H(network, height, view)`
- Persist beacon with block
- Private net + testnet soak

**Exit:** Testnet nodes produce beacons every block; recovery/view-change covered.

### Phase 3 — ApplicationEngine + HF

**Work:**

- New hardfork enum value (e.g. next after Huyao — TC names it)
- `GetRandom` switches to PRF(beacon, …)
- `GetBlockBeacon` optional interop
- Replay tests: historical pre-HF vs post-HF
- Gas pricing unchanged or Policy-tuned

**Exit:** Unit + integration tests green; public testnet dApps can use new semantics.

### Phase 4 — PUA tooling & docs

**Work:**

- Developer docs on neo.dev / developers.neo.org
- Optional analyzer / IVD lite
- Example contracts (fair NFT, multi-random draw) using TemporaryStorage + RNP if both ship

**Exit:** Documented patterns; no critical open PUA footguns in samples.

### Phase 5 — Mainnet HF

**Work:**

- Mainnet height schedule
- Monitoring: beacon miss rate, aggregation latency, GetRandom volume
- Incident runbook if beacon aggregation fails

---

## 9. Repository / module split

| Repo | Changes |
|------|---------|
| **neo-project/neo** | Crypto helpers, ApplicationEngine, Policy, header/payload types, unit tests, HF enum |
| **neo-project/neo-modules** (or consensus host) | dBFT message handling, partial exchange, timeouts |
| **neo-project/neo-dev-portal** | User docs |
| **neo-project/proposals** | NEP text |

Graphite / stacked PRs recommended:

1. Crypto primitives  
2. Payload + HF plumbing (no behavior change)  
3. Engine GetRandom switch (tests with injected beacon)  
4. Consensus plugin (depends on 2)

---

## 10. Testing strategy

### 10.1 Unit

- PRF domain separation (different tx / counter / network)
- Beacon combine determinism (shuffled partial order)
- Invalid partial rejection
- View change: different `rn` ⇒ different beacon
- HF gate: pre-fork GetRandom byte-compatible with current mainnet rules

### 10.2 Consensus / network

- 7-node private net: fault 0, 1, 2 CNs offline during partial collection
- View change under load
- Measure aggregation µs vs paper (~318 µs at n=7, k=4)

### 10.3 Application

- Contract uses GetRandom 1…N times; all nodes same results
- PUA sample contracts (expected insecure without IVD)
- Fee regression vs two-tx commit–execute pattern

### 10.4 Replay

- Full chain sync across HF height
- State root equality on all nodes

---

## 11. Risks and open decisions

| Risk | Mitigation |
|------|------------|
| Consensus latency regression | Cap partial size; async crypto; measure on testnet |
| Beacon missing under faults | Explicit Policy: fail block vs insecure fallback |
| Header bloat | Fixed-size field; no per-tx proofs on-chain |
| Wrong `k` | Mainnet conservative default; committee Policy if safe |
| PUA residual | Docs + optional IVD; cannot fully ban economic griefing |
| Scope creep into full VRF product | Stay DRB+PRF; out of scope: user-key VRF like Chainlink |
| Credit / IP | Cite paper and **@Jim8y** in NEP, PR, and release notes |

### TC decision checklist

- [ ] Accept single-round RNP as roadmap item?  
- [ ] Choose `k` and failover policy  
- [ ] Header field vs native-only beacon storage  
- [ ] Whether Script PUA IVD is in-scope for v1  
- [ ] Hardfork naming / ordering relative to Huyao and other forks  
- [ ] Ownership: core vs NeoResearch / community co-maintain

---

## 12. Success metrics

| Metric | Target |
|--------|--------|
| Randomness latency | 1 consensus round (same block as request) |
| Consensus overhead | ≪ 1% block time at 15s (paper ~0.002%) |
| Fee vs commit–execute | Order-of-magnitude cheaper for multi-draw txs |
| Beacon availability | > 99.9% blocks with valid beacon on testnet |
| Replay safety | 100% agreement on GetRandom streams post-HF |
| PUA docs | Published before mainnet HF |

---

## 13. Comparison with current Neo `GetRandom`

| | Today (Aspidochelone+) | This plan (BFTRAND-adapted) |
|--|------------------------|-----------------------------|
| Entropy source | Tx hash + Murmur | Threshold DRB beacon + PRF |
| Adversary model | Weak (tx author influence on seed) | BFT `f < n/3` + threshold |
| Rounds | 1 (but weak) | 1 (stronger) |
| Extra txs | 0 | 0 |
| View-change care | N/A | Required |
| PUA | Unaddressed | Documented + optional IVD |
| HF | Already forked once | New HF for beacon path |

---

## 14. Suggested NEP outline

1. Abstract & motivation  
2. Credits (**@Jim8y**, BFTRAND / DSN 2024)  
3. Definitions (beacon, partial, PRF, PUA)  
4. Consensus changes  
5. Block / storage format  
6. NeoVM interop semantics  
7. Pricing & Policy  
8. Security considerations  
9. Backwards compatibility & HF  
10. Test vectors  
11. References  

---

## 15. Immediate next steps (actionable)

1. Open/track discussion issue on `neo-project/neo` (link this design).  
2. Confirm credit line with **[@Jim8y](https://github.com/Jim8y)** and paper citation format.  
3. TC: approve Phase 0 NEP skeleton.  
4. Spike: BLS threshold partial/combine using existing `Neo.Cryptography.BLS12_381` package.  
5. Spike: inject fake beacon into `ApplicationEngine` unit tests to prototype PRF `GetRandom` without full consensus.  

---

## 16. References

1. BFTRAND: *Low-latency Random Number Provider for BFT Smart Contracts*, DSN 2024 (paper389).  
2. [@Jim8y](https://github.com/Jim8y) — research credit.  
3. Neo N3 dBFT / NeoVM documentation (`developers.neo.org`).  
4. `ApplicationEngine.GetRandom` — `src/Neo/SmartContract/ApplicationEngine.Runtime.cs`.  
5. Related Neo topics: dBFT 3.0 / Double Speakers (#2029, #3254), Proof of Node (#4542).  
6. DRB literature cited by BFTRAND (Dfinity, Drand, BLS threshold schemes).  

---

## Appendix A — Worked example (mainnet-shaped)

```text
n = 7, t = 2, k = 5
height = 12_000_000, view = 0
rn = Hash(0x4e454f33 ‖ 12_000_000 ‖ 0)

CNs 0..6 each produce partial (σ_i, π_i)
Any 5 valid partials → σ

Tx T in block calls GetRandom twice:
  r0 = PRF(σ, T.hash, 0)
  r1 = PRF(σ, T.hash, 1)

All honest nodes produce identical r0, r1.
Attacker with ≤2 CNs cannot assemble alternate valid σ.
```

## Appendix B — Credit block (for NEP / PR templates)

```markdown
### Credits
- Research foundation: BFTRAND (DSN 2024) by [Jim8y](https://github.com/Jim8y) et al.
- Neo adaptation design: <this document / PR authors>
```

---

*End of design draft.*
