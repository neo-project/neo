# How to: run a Neo node

This is the practical path using **[neo-project/neo-node](https://github.com/neo-project/neo-node)**. You do not start a node from the `neo` protocol repo alone.

## Option A — pre-built release

1. Open [neo-node releases](https://github.com/neo-project/neo-node/releases) (not the old `neo-project/neo-cli` repo).
2. Download the zip for your OS.
3. Unzip. On Linux/macOS you may need `chmod +x` on the binary.
4. Put at least one **storage plugin** in `Plugins/` if the zip does not already include it (LevelDBStore or RocksDBStore).
5. Run:

```bash
./neo-cli          # Linux / macOS
# or double-click neo-cli.exe on Windows
```

You should get a `neo>` prompt. Run `show state`. Height will climb as peers send blocks.

## Option B — compile from source (verified against current tree)

Prerequisites: Git, **.NET 10 SDK**.

**Linux packages** (for LevelDB; names vary):

```bash
# Debian/Ubuntu (current, not only 14/16/18)
sudo apt-get install libleveldb-dev sqlite3 libsqlite3-dev
```

**macOS:**

```bash
brew install leveldb
```

**Windows:** use the [Neo LevelDB build](https://github.com/neo-project/leveldb) and place `libleveldb.dll` next to the CLI if the plugin requires it.

Clone and publish:

```bash
git clone https://github.com/neo-project/neo-node.git
cd neo-node
git checkout master-n3

dotnet publish src/Neo.CLI -c Release -o ./publish
cd publish
```

Copy plugin output into `./publish/Plugins/<PluginName>/` (each plugin’s `dotnet publish` output, including its `plugin.json` / `config.json`). Minimum for a syncing node:

- `LevelDBStore` **or** `RocksDBStore`
- optional: `RpcServer`, `ApplicationLogs`

Start:

```bash
dotnet Neo.CLI.dll
# or the published executable name on your OS
```

## First commands

```text
neo> help
neo> show state
neo> plugins
```

Create a wallet only if you need to send or deploy (not required just to sync):

```text
neo> create wallet wallet.json
neo> open wallet wallet.json
```

## RPC

Install/enable **RpcServer**, then from another terminal:

```bash
curl -X POST http://127.0.0.1:10332 \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"getblockcount","params":[],"id":1}'
```

TestNet uses **20332**. API catalog: [RPC methods](https://developers.neo.org/docs/n3/reference/rpc/latest-version/api).

Do not expose 10332 to the world with an open wallet.

## Consensus (private net)

You need **DBFTPlugin**, a wallet that holds the consensus keys, matching `protocol.json` / `config.json`, and:

```text
neo> open wallet <cn-wallet.json>
neo> start consensus
```

Public MainNet consensus is for elected CNs. For local experiments use [neo-express](https://github.com/neo-project/neo-express) unless you are testing the real CLI.

## Docker

From neo-node:

```bash
cd src/Neo.CLI/Docker
docker build -t neo-cli .
docker run -p 10332:10332 -p 10333:10333 --name=neo-cli-mainnet neo-cli
```

Attach with `docker exec -it …` as described in the neo-node README.

## If it does not start

| Symptom | Check |
| --- | --- |
| Immediate exit, no prompt | Missing `config.json`, or wrong working directory |
| “no storage provider” | No LevelDB/RocksDB plugin in `Plugins/` |
| Native DLL error | LevelDB not on PATH / not next to the binary |
| RPC connection refused | RpcServer plugin not loaded; `plugins` command |
| Height stuck at 0 | P2P port 10333 firewalled; seeds in config |

## Next

- [Neo node overview](../neo-node.md)
- [Smart contracts](smart-contracts.md)
- [CLI command reference](https://developers.neo.org/docs/n3/node/cli/cli)
