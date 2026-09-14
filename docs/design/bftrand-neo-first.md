# BFTRAND / RNP — `neo` first, `neo-node` later

Tracking: [neo-project/neo#4724](https://github.com/neo-project/neo/issues/4724)  
Full design: [bftrand-neo-rnp.md](bftrand-neo-rnp.md)

**Yes — this library can be made ready before any [neo-node](https://github.com/neo-project/neo-node) work.** Consensus message exchange lives in **DBFTPlugin** (`neo-node`). The protocol types, crypto, hardfork, and `GetRandom` switch live here.

This does **not** replace Technical Committee decisions (hardfork name, `k`, header vs native storage, failover). It is the engineering split so neo-node can consume a stable API.

## Why neo first

```
neo-node (DBFTPlugin)                 neo (this repo)
─────────────────────                 ────────────────
collect BLS partials          ──►     ThresholdBls.Combine / Verify
attach beacon to block        ──►     Header / payload field (ISerializable)
                                      ApplicationEngine.GetRandom PRF
                                      HF gate + tests with injected beacon
```

neo-node cannot produce a beacon until **types + crypto** exist. The engine can be tested **without** a real CN set by injecting a fake 32-byte beacon in unit tests.

## Work that belongs in `neo-project/neo` (this branch)

Do these **before** opening neo-node PRs:

| Order | Item | Notes |
| --- | --- | --- |
| 1 | Crypto: threshold BLS partial / combine / verify | Use existing BLS12-381; unit tests for shuffled combine order and bad partials |
| 2 | Wire types: beacon bytes + optional `BeaconPartial` | `ISerializable`; no dBFT behavior yet |
| 3 | Block commitment plumbing | Header extension **or** native store — wait for TC; until then keep types unused on the wire (`Size` tests only) |
| 4 | `ApplicationEngine.GetRandom` PRF path | `PRF(beacon, txHash, counter)`; **HF-gated**; missing beacon policy stub |
| 5 | Optional `GetBlockBeacon` interop | Same HF |
| 6 | Tests | Pre-HF Murmur byte-compatible; post-HF injected beacon domain separation |

## Work that must wait for `neo-project/neo-node`

| Item | Why |
| --- | --- |
| dBFT `Prepare` / `PrepareResponse` / `Commit` carrying partials | Plugin, not this library |
| Collect ≥ `k` partials, view-change discard | Consensus state machine |
| Timeouts if `k` missing | Node / plugin Policy |
| Private-net soak, CN configs | Node |

Do **not** start neo-node until (1)+(2) compile and (4) passes with an injected beacon.

## Do not do yet

- Name a new `Hardfork` enum value until TC names it (do not reuse `HF_Iara` unless they say so)
- Change MainNet `GetRandom` semantics
- Full Script-PUA / IVD (design Phase R1–R2)

## Suggested PR stack on `neo`

1. Crypto primitives only  
2. Payload types (no behavior)  
3. Engine switch + tests with injected beacon  

Then neo-node: consensus plugin depending on PR 2.

## Current `GetRandom` (baseline)

`ApplicationEngine.GetRandom` (`ApplicationEngine.Runtime.cs`): post-`HF_Aspidochelone` uses Murmur128 over `nonceData` and `Network + randomTimes`. That path stays for pre-RNP heights.
