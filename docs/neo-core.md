# Neo core library (`neo-project/neo`)

This is the repository you are in. It implements the **Neo N3 protocol in C#**: state, networking, native contracts, and the host that runs NeoVM.

It is **not** the process that binds ports 10332/10333. That process is [Neo-CLI in neo-node](neo-node.md).

- Source: [github.com/neo-project/neo](https://github.com/neo-project/neo)
- Default N3 branch: **`master-n3`**
- Next generation: **`master`** (NEO-4)
- Current package prefix: **3.10.2**, target **net10.0** (`src/Directory.Build.props`)

## What this library is responsible for

- **Protocol settings** — magic, seed list, hardfork heights (`ProtocolSettings`)
- **Blocks and transactions** — headers, witnesses, attributes
- **Ledger** — persist blocks, apply native contract `OnPersist` / `PostPersist`
- **Mempool** — unverified vs verified transactions
- **P2P** — inventory, getdata, headers/blocks sync (`TaskManager`)
- **Persistence API** — `IStore` / `DataCache` (the *engine* is a node plugin)
- **Smart contract host** — `ApplicationEngine`, interop, native contracts
- **Wallets** — NEP-6 JSON, accounts, signing (used by CLI)

## Project layout (GitHub `master-n3`)

On the official GitHub tree, `src/` contains:

| Project | Purpose |
| --- | --- |
| `Neo` | Protocol implementation |
| `Neo.Extensions` | helpers |
| `Neo.IO` | `ISerializable`, caches |
| `Neo.Json` | JSON tokens used by RPC/wallets |

Some local checkouts (including developer forks) may also contain `Neo.CLI` or plugins. Treat those as **local extras**. The published split is neo / neo-node / neo-vm.

A more detailed folder list is in the root [README](../README.md#project-structure).

## Native contracts (from current source)

Implemented under `src/Neo/SmartContract/Native/`:

| Contract | Role |
| --- | --- |
| `ContractManagement` | Deploy / update / destroy contracts |
| `StdLib` | `serialize`, `jsonDeserialize`, `stringSplit`, base58, … |
| `CryptoLib` | hashes, signature verify, BLS12-381 |
| `LedgerContract` | query blocks and transactions from contracts |
| `NeoToken` | NEO asset, voting, GAS generation |
| `GasToken` | GAS asset |
| `PolicyContract` | fees, blocked accounts, exec fee factor |
| `RoleManagement` | oracle / state validator / notary roles |
| `OracleContract` | native oracle requests |
| `Notary` | notary assistance |
| `Treasury` | treasury |

Name Service is **not** in this list. It lives in [non-native-contracts](https://github.com/neo-project/non-native-contracts). Older CLI dumps on developers.neo.org still show `NameService` next to natives — that is stale ([notes](reference/official-docs.md)).

## Hardforks

`Hardfork` in `src/Neo/Hardfork.cs` currently includes Aspidochelone through **Iara**. Heights are configured per network in protocol settings. A feature gated on `HF_Huyao` does nothing until that height.

When you change VM or fee behavior, always ask: **is this a hardfork?** If yes, gate it and add tests with the fork disabled *and* enabled.

## ApplicationEngine vs NeoVM

```
Contract NEF
    → ApplicationEngine.Execute()     // neo: culture, fees, snapshot
        → ExecutionEngine (NeoVM)     // neo-vm: opcodes
            → SYSCALL
                → interop in ApplicationEngine  // storage, CheckWitness, …
```

Changing `ADD` belongs in **neo-vm**. Changing `System.Storage.Put` or opcode *prices* in the host belongs in **neo**.

## Persistence

The library defines **interfaces**. LevelDB and RocksDB are **plugins in neo-node**. In-memory `MemoryStore` is used by unit tests.

Read [Persistence architecture](persistence-architecture.md) after this page.

## How to work in this repo

```bash
git clone https://github.com/neo-project/neo.git
cd neo
git checkout master-n3
dotnet test tests/Neo.UnitTests/Neo.UnitTests.csproj
```

More: [How to: build and test](how-to/build-and-test.md).

## Related reading

- [Getting started](getting-started.md)
- [Architecture](architecture.md)
- [Serialization format](serialization-format.md)
- [Contribute](how-to/contribute.md)
- Unit tests as examples: `tests/Neo.UnitTests/SmartContract/Native/`
