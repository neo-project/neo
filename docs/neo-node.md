# Neo node (`neo-project/neo-node`)

Neo-CLI is the full-node program: P2P, optional RPC, optional consensus, wallet commands.

- Source: [github.com/neo-project/neo-node](https://github.com/neo-project/neo-node)
- N3 branch: **`master-n3`**
- Solution: `neo-node.sln`
- Targets **net10.0**, version prefix **3.10.2** (same generation as this library)

This documentation lives in **neo-project/neo** so beginners can see the whole stack in one place. Changes to CLI or plugins are still submitted on the **neo-node** repository.

## What you get

From the [official node introduction](https://developers.neo.org/docs/n3/node/Introduction) (feature table is still useful):

| | Neo-CLI (this repo) | Neo-GUI |
| --- | --- | --- |
| Interface | Command line | Graphical |
| Wallet / transfer / vote | Yes | Yes |
| JSON-RPC | Yes (RpcServer plugin) | No |
| Participate in consensus | Yes (DBFTPlugin) | No |

GUI for N3 is maintained separately: [neo-ngd/Neo3-GUI](https://github.com/neo-ngd/Neo3-GUI). The historical **neo-gui** folder inside neo-node is not the app most users download.

## Layout on GitHub

```
neo-node/
  src/Neo.CLI/            # the executable project
  src/Neo.ConsoleService/ # prompt, commands
  plugins/                # optional DLLs
    RpcServer/
    DBFTPlugin/
    LevelDBStore/
    RocksDBStore/
    ApplicationLogs/
    OracleService/
    StateService/
    TokensTracker/
    StorageDumper/
    RestServer/
    SQLiteWallet/
    SignClient/
    …
  tests/
```

CLI **depends on** `neo-project/neo` (this library) and **neo-vm**.

## Plugins you will actually use

| Plugin | Why |
| --- | --- |
| **LevelDBStore** or **RocksDBStore** | Persist the chain. Without one, many setups cannot keep state on disk. |
| **RpcServer** | `getblock`, `invokescript`, wallets over HTTP |
| **ApplicationLogs** | Contract `notify` logs (needed for many indexers) |
| **DBFTPlugin** | `start consensus` on a private net or as a CN |
| **TokensTracker** | NEP-17 balance RPCs |
| **OracleService** | If the node provides oracle |
| **StateService** | MPT state root / proofs |

Load plugins by putting their published output in `Plugins/<Name>/` next to the CLI, or use the `install <Name>` command (downloads a release zip; restart CLI).

## Ports (still correct)

| | MainNet | TestNet |
| --- | --- | --- |
| RPC HTTP | **10332** | **20332** |
| RPC HTTPS | 10331 | 20331 |
| P2P TCP | **10333** | **20333** |
| P2P WS | 10334 | 20334 |

**Security:** Neo-CLI does not require a password to call RPC after the wallet is open, and it does not remotely lock the wallet. Bind RPC to localhost or put it behind a firewall. This warning on developers.neo.org is still valid.

## Commands worth knowing first

Full tables: [CLI command reference](https://developers.neo.org/docs/n3/node/cli/cli) (command names match current CLI).

After `neo>` appears:

```text
help
show state
plugins
create wallet wallet.json
open wallet wallet.json
list nativecontract
```

Wallet must be open for `send`, `deploy`, `invoke`, `start consensus`.

## Verified vs stale (neo-node README)

The neo-node README on `master-n3` still says:

- “Install .NET Core” and “Ubuntu LTS 14, 16 and 18”
- `cd neo-node/neo-cli` then `dotnet neo-cli.dll`

**Current source:**

- SDK is **.NET 10**, project is `src/Neo.CLI`
- Publish with `dotnet publish src/Neo.CLI -c Release`
- Run the published `Neo.CLI` / `neo-cli` binary from the output directory, with `config.json` and `Plugins/`

Step-by-step: [How to: run a node](how-to/run-a-node.md).

## Docker

neo-node ships a Docker context under `src/Neo.CLI/Docker` (see that README). Typical ports: `10332` (RPC) and `10333` (P2P).

## Logging and fast sync

- Logs: ApplicationLogs plugin
- Faster catch-up: offline block files — [sync blocks](https://docs.neo.org/docs/en-us/node/syncblocks.html) (older docs URL; the idea is still “import a chain dump instead of P2P from genesis”)

## Next

- [How to: run a node](how-to/run-a-node.md)
- [Architecture](architecture.md)
- [Neo core](neo-core.md)
