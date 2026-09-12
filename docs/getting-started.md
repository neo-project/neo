# Getting started

This page is for people who have never worked on Neo internals. By the end you should know what this repository is, what it is *not*, and which guide to open next.

## What is Neo?

Neo is an open-source public blockchain. Applications (smart contracts) run on every full node and must produce the **same result** on every machine. That is why the virtual machine is deterministic: given the same script and the same chain state, every node must agree.

Two native assets ship with the chain:

| Token | What it is | Typical use |
| --- | --- | --- |
| **NEO** | Governance token | Voting for committee / consensus |
| **GAS** | Utility token | Paying network and system fees |

On N3, GAS is claimed automatically when the NEO balance of an account changes. You do not run a separate “claim GAS” ritual like Neo 2.

Official concept pages (still accurate for N3):

- [Introduction to Neo](https://developers.neo.org/docs/n3/foundation/introduction)
- [Native tokens](https://developers.neo.org/docs/n3/foundation/Native%20tokens)
- [Governance](https://developers.neo.org/docs/n3/foundation/governance)
- [dBFT](https://developers.neo.org/docs/n3/foundation/consensus/consensus_algorithm)

## Three GitHub repos, not one program

Beginners often clone this repository and expect a running node. **This repo is the protocol library.** The program you start is **Neo-CLI**, which lives in a different repository.

| Repository | What it contains | You clone it when… |
| --- | --- | --- |
| [neo-project/neo](https://github.com/neo-project/neo) (**this repo**) | Ledger, P2P messages, native contracts, `ApplicationEngine`, wallets, persistence interfaces | You change consensus rules, fees, storage keys, or native contracts |
| [neo-project/neo-node](https://github.com/neo-project/neo-node) | `Neo.CLI` (the process), console commands, plugins (RPC, DBFT, LevelDB/RocksDB, Oracle, …) | You want to **run** a node, expose JSON-RPC, or change CLI/plugins |
| [neo-project/neo-vm](https://github.com/neo-project/neo-vm) | Stack VM: opcodes, evaluation stack, execution engine **without** blockchain syscalls | You change instruction semantics or embed the VM in another host |

A full node is: **neo-node process + neo library + neo-vm + a storage plugin**.

See [Architecture](architecture.md) for a picture of the call path from “RPC invoke” down to an opcode.

## What you need on your machine

- **OS:** Windows, Linux, or macOS
- **SDK:** [.NET 10](https://dotnet.microsoft.com/download) (this tree’s projects use `net10.0`; package version prefix is `3.10.2`)
- **Git**
- For a **persisted** node on Linux: LevelDB or RocksDB native libraries (see [Run a node](how-to/run-a-node.md))

`dotnet --version` should report a 10.x SDK.

## A 10-minute tour of *this* repo

```bash
git clone https://github.com/neo-project/neo.git
cd neo
git checkout master-n3          # N3 protocol line
dotnet test tests/Neo.UnitTests/Neo.UnitTests.csproj
```

That builds the core library and runs unit tests. It does **not** start a P2P node.

Useful folders:

| Folder | Role |
| --- | --- |
| `src/Neo/` | Protocol: blockchain actor, mempool, native contracts, engine |
| `src/Neo.IO/` | Serialization and caches |
| `src/Neo.Json/` | JSON used by RPC and wallets |
| `src/Neo.Extensions/` | Shared helpers |
| `tests/` | Unit tests |

N3 work happens on **`master-n3`**. The **`master`** branch is the next-generation (NEO-4) line. See [Contribute](how-to/contribute.md).

## A 10-minute tour of the *node*

The CLI is not in this repository on GitHub. Clone the node repo:

```bash
git clone https://github.com/neo-project/neo-node.git
cd neo-node
git checkout master-n3
```

Then follow [How to: run a node](how-to/run-a-node.md). After start you should see a `neo>` prompt. Type `help` and `show state`.

## A 10-minute tour of the *VM*

```bash
git clone https://github.com/neo-project/neo-vm.git
cd neo-vm
git checkout master
dotnet test tests/Neo.VM.Tests
```

The VM does not know about blocks. Blockchain features (storage, `System.Runtime.*` syscalls) are added by `ApplicationEngine` in **this** repo. Details: [NeoVM](neo-vm.md).

## Common mix-ups

| Mix-up | Reality |
| --- | --- |
| “I cloned `neo` but `dotnet neo-cli.dll` is missing” | CLI is in [neo-node](https://github.com/neo-project/neo-node). |
| “Official page says download `neo-cli` from `neo-project/neo-cli`” | That extra repo is historical. Current source is **neo-node**. Releases are under [neo-node/releases](https://github.com/neo-project/neo-node/releases). |
| “NameService is a native contract in the CLI dump I found online” | In current `neo` source, native contracts include ContractManagement, StdLib, CryptoLib, Ledger, NeoToken, GasToken, Policy, RoleManagement, Oracle, Notary, Treasury. Name service is a [non-native contract](https://github.com/neo-project/non-native-contracts). |
| “I need Ubuntu 16 and .NET Core” | Current projects target **.NET 10**. The neo-node README still mentions old Ubuntu LTS numbers; any current Linux with the SDK works. |
| “I will implement a dApp in this repo” | Application code is compiled with [neo-devpack-dotnet](https://github.com/neo-project/neo-devpack-dotnet) and deployed *to* the chain. This repo *is* the chain. |

## Next step

- New to blockchain terms → [Glossary](glossary.md)
- Want the big picture → [Architecture](architecture.md)
- Ready to type commands → [How to: build and test](how-to/build-and-test.md)
