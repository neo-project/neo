# Glossary

Short definitions used in these docs. Official longer pages are linked where they still match the source.

| Term | Meaning |
| --- | --- |
| **N3** | Current Neo protocol generation. Developed on `master-n3` in `neo` and `neo-node`. |
| **NEO-4 / `master`** | Next protocol line in `neo-project/neo` (`master` branch). Do not mix PRs between `master` and `master-n3`. |
| **Full node** | A process that stores the chain, speaks P2P, and can verify every block. Neo-CLI is a full node. |
| **Neo-CLI** | Command-line full node. Source: [neo-project/neo-node](https://github.com/neo-project/neo-node) (`src/Neo.CLI`). |
| **Plugin** | DLL dropped into Neo-CLI’s `Plugins/` folder (RPC, storage engine, consensus, oracle, logs). |
| **dBFT** | Delegated Byzantine Fault Tolerance — how N3 agrees on the next block. [Official page](https://developers.neo.org/docs/n3/foundation/consensus/consensus_algorithm). |
| **Committee** | Elected accounts that vote on network parameters. |
| **Validators / consensus nodes** | The subset that actually produce blocks (via DBFTPlugin). |
| **Mempool** | In-memory list of unconfirmed transactions (`MemoryPool` in this repo). |
| **GAS** | Fee token. Pays network fee (size / verification) and system fee (VM execution). |
| **NEO** | Governance token; voting weight. |
| **datoshi** | 10⁻⁸ GAS (smallest typical unit in APIs). |
| **Script hash / UInt160** | 20-byte contract or account identifier. Addresses like `N…` are a Base58 form of a script hash. |
| **UInt256** | 32-byte hash (block hash, tx hash). |
| **NEF** | Neo Executable Format — compiled contract bytecode file (`.nef`). |
| **Manifest** | `*.manifest.json` describing contract methods, permissions, events. |
| **NeoVM** | Stack machine that runs NEF. Repo: [neo-project/neo-vm](https://github.com/neo-project/neo-vm). |
| **Opcode** | One VM instruction (`PUSH1`, `SYSCALL`, `RET`, …). |
| **Syscall / interop** | Call from the VM into the host (`System.Runtime.CheckWitness`, storage, …) implemented in `ApplicationEngine`. |
| **ApplicationEngine** | Neo’s host around NeoVM: fees, syscalls, snapshots. Lives in **this** repo, not neo-vm. |
| **Native contract** | Built-in contract (NEO, GAS, Policy, …) implemented in C# in `src/Neo/SmartContract/Native`. |
| **Hardfork** | Named protocol upgrade (`HF_Huyao`, `HF_Gorgon`, …) activated at a block height. |
| **Snapshot / `DataCache`** | Isolated view of storage for one execution or block persist. See [Persistence](persistence-architecture.md). |
| **StorageKey** | `contract id + key bytes` used in the store. |
| **RPC** | JSON-HTTP API (`getblock`, `invokescript`, …). Plugin: RpcServer. Ports: see [Neo node](neo-node.md). |
| **Witness** | Signature (or contract) that authorizes a transaction. |
| **NEP** | Neo Enhancement Proposal — [neo-project/proposals](https://github.com/neo-project/proposals). NEP-17 is the fungible token standard. |
