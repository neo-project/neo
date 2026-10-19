# Neo documentation

This folder is a **beginner-friendly guide** to the C# Neo stack: how the pieces fit together, how to build them, and how to run a node.

Also listed from the repository root: [README.md — Table of Contents](../README.md#table-of-contents).

You do **not** need to have read the protocol papers first. Start with [Getting started](getting-started.md), then pick a how-to.

## Read in this order

| If you want to… | Open this |
| --- | --- |
| Understand Neo in plain language | [Getting started](getting-started.md) |
| Look up a word (GAS, dBFT, NEF, …) | [Glossary](glossary.md) |
| See how the three GitHub repos fit together | [Architecture](architecture.md) |
| Work on **this** repository (`neo-project/neo`) | [Neo core library](neo-core.md) |
| Run or customize a full node (`neo-project/neo-node`) | [Neo node](neo-node.md) |
| Understand the virtual machine (`neo-project/neo-vm`) | [NeoVM](neo-vm.md) |
| Build the code and run tests | [How to: build and test](how-to/build-and-test.md) |
| Start Neo-CLI and sync | [How to: run a node](how-to/run-a-node.md) |
| Compile and deploy a contract | [How to: smart contracts](how-to/smart-contracts.md) |
| Open a pull request | [How to: contribute](how-to/contribute.md) |

## In-repo reference (advanced)

These two files describe internals. They assume you already know what a block and a `StorageKey` are.

- [Persistence architecture](persistence-architecture.md) — stores, snapshots, `DataCache`
- [Serialization format](serialization-format.md) — `ISerializable`, VarInt, little-endian integers

## Official sites (verified)

| Site | Use it for | Watch out |
| --- | --- | --- |
| [developers.neo.org](https://developers.neo.org/) | Concepts, RPC, CLI commands, contract APIs | Some **download links still point at `neo-cli` as its own repo**. The CLI now lives in [neo-project/neo-node](https://github.com/neo-project/neo-node). |
| [docs.neo.org](https://docs.neo.org/) | Older tutorials linked from the README | Prefer developers.neo.org for N3. |
| [neo.org](https://neo.org/) | Product / community | Not a protocol spec. |

Details of what we checked against the current source trees: [Official docs notes](reference/official-docs.md).

## The three repositories this guide covers

```
┌─────────────────────┐     uses      ┌─────────────────────┐
│  neo-project/neo    │◄──────────────│ neo-project/neo-node│
│  Protocol library   │               │ Neo-CLI + plugins   │
│  Ledger, P2P, GAS   │               │ RPC, DBFT, storage  │
└─────────┬───────────┘               └─────────────────────┘
          │ hosts
          ▼
┌─────────────────────┐
│ neo-project/neo-vm  │
│ Stack VM + opcodes  │
└─────────────────────┘
```

All three currently target **.NET 10** and version **3.10.x** on the N3 line (`master-n3` for neo and neo-node; `master` for neo-vm).
