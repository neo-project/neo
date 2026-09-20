# Architecture

How [neo](https://github.com/neo-project/neo), [neo-node](https://github.com/neo-project/neo-node), and [neo-vm](https://github.com/neo-project/neo-vm) work as one stack.

## Mental model

Think of three layers:

1. **NeoVM** (`neo-vm`) — a calculator. It has stacks and opcodes. It does not know what a block is.
2. **Neo core** (`neo`) — the blockchain: blocks, transactions, P2P messages, native contracts, and `ApplicationEngine` (the VM *plus* fees and syscalls).
3. **Neo-CLI** (`neo-node`) — the process you run: config files, console, plugins that attach storage, RPC, and consensus to the core.

```
You  →  Neo-CLI (neo-node)
            │  loads plugins (RpcServer, LevelDBStore, DBFTPlugin, …)
            ▼
        NeoSystem  (neo)
            ├── Blockchain actor / MemoryPool / Ledger
            ├── P2P LocalNode
            ├── Native contracts
            └── ApplicationEngine  ──executes──►  NeoVM (neo-vm)
```

## What happens when a contract is invoked

1. A client calls RPC `invokescript` or `invokefunction` (RpcServer plugin in **neo-node**).
2. The node builds an `ApplicationEngine` (**neo**) with a store snapshot.
3. The engine loads the contract’s NEF script into NeoVM (**neo-vm**).
4. Opcodes run on the evaluation stack. `SYSCALL` leaves the VM and runs C# interop in `ApplicationEngine`.
5. Storage reads/writes go through `DataCache` → `IStore` (LevelDB/RocksDB plugin).
6. If this was a real transaction (not a dry-run invoke), the tx sits in the mempool until dBFT includes it in a block.

That split is why a VM PR and a native-contract PR land in different repositories.

## Repository map

### neo-project/neo (this repo)

| Area | Path | Notes |
| --- | --- | --- |
| Protocol types | `src/Neo/` | `UInt160`, `Hardfork`, `ProtocolSettings` |
| Ledger | `src/Neo/Ledger/` | `Blockchain`, `MemoryPool` |
| P2P | `src/Neo/Network/` | messages, `RemoteNode`, `TaskManager` |
| Persistence | `src/Neo/Persistence/` | `IStore`, `DataCache` — engines are plugins |
| Smart contracts | `src/Neo/SmartContract/` | `ApplicationEngine`, native contracts |
| Wallets | `src/Neo/Wallets/` | NEP-6 JSON wallets |
| IO / JSON | `src/Neo.IO`, `src/Neo.Json` | serialization, RPC JSON |

NuGet: packages named `Neo`, `Neo.IO`, `Neo.Json`, `Neo.Extensions`.

### neo-project/neo-node

| Area | Path | Notes |
| --- | --- | --- |
| CLI | `src/Neo.CLI/` | `dotnet Neo.CLI.dll` (or `neo-cli` binary) |
| Console host | `src/Neo.ConsoleService/` | command loop |
| Plugins | `plugins/` | RpcServer, DBFTPlugin, LevelDBStore, RocksDBStore, OracleService, ApplicationLogs, StateService, TokensTracker, … |

Neo-CLI **references the `Neo` library**. On GitHub this is a package or project reference to `neo-project/neo`, not a copy of all protocol code.

### neo-project/neo-vm

| Area | Path | Notes |
| --- | --- | --- |
| VM | `src/Neo.VM/` | `ExecutionEngine`, `Instruction`, jump tables |
| Tests | `tests/Neo.VM.Tests/` | opcode tests |

NuGet: `Neo.VM` (`dotnet add package Neo.VM`).

## Consensus vs library

dBFT lives as **DBFTPlugin** in **neo-node**. The library still defines payloads and the ledger. You can run a node that only syncs (no `start consensus`) by omitting that plugin.

## Networks and ports

Verified against the current [node introduction](https://developers.neo.org/docs/n3/node/Introduction) (ports are still right; the *source* links on that page are stale — see [official docs notes](reference/official-docs.md)).

| Service | MainNet | TestNet |
| --- | --- | --- |
| JSON-RPC HTTP | 10332 | 20332 |
| JSON-RPC HTTPS | 10331 | 20331 |
| P2P TCP | 10333 | 20333 |
| P2P WebSocket | 10334 | 20334 |

RPC must **not** be opened to the internet without a firewall or wallet isolation. Neo-CLI does not authenticate `open wallet`.

## Where to change what

| You want to change… | Open a PR on |
| --- | --- |
| GAS fees, native `StdLib`, hardfork behavior | [neo](https://github.com/neo-project/neo) |
| Opcode pricing inside the VM itself | [neo-vm](https://github.com/neo-project/neo-vm) (and often neo together) |
| `getblock` RPC shape, CLI `send` command | [neo-node](https://github.com/neo-project/neo-node) |
| C# → NEF compiler | [neo-devpack-dotnet](https://github.com/neo-project/neo-devpack-dotnet) |
| A standard (NEP-17, …) | [proposals](https://github.com/neo-project/proposals) |
