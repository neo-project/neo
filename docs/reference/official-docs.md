# Official docs notes

We used [developers.neo.org](https://developers.neo.org/) while writing these guides and checked the **current GitHub trees**. This page records mismatches so you do not follow a dead link.

## Still accurate

| Topic | URL | Check |
| --- | --- | --- |
| Platform intro | [foundation/introduction](https://developers.neo.org/docs/n3/foundation/introduction) | Matches N3 product story |
| NeoVM architecture | [foundation/neovm](https://developers.neo.org/docs/n3/foundation/neovm) | Matches neo-vm README + `ExecutionEngine` |
| CLI commands | [node/cli/cli](https://developers.neo.org/docs/n3/node/cli/cli) | Command names (`show state`, `deploy`, `invoke`, …) still match Neo-CLI |
| RPC catalog | [reference/rpc](https://developers.neo.org/docs/n3/reference/rpc/latest-version/api) | Use as API index |
| Node ports | [node/Introduction](https://developers.neo.org/docs/n3/node/Introduction) | 10332/10333 MainNet, 20332/20333 TestNet |
| CLI vs GUI feature table | same page | Still the right split (RPC + consensus = CLI) |
| dBFT / governance | [consensus](https://developers.neo.org/docs/n3/foundation/consensus/consensus_algorithm), [governance](https://developers.neo.org/docs/n3/foundation/governance) | Concept pages |

## Stale or easy to get wrong

| What you might see | Current reality |
| --- | --- |
| Download / source **neo-project/neo-cli** | CLI source is **[neo-project/neo-node](https://github.com/neo-project/neo-node)** (`src/Neo.CLI`). Releases: [neo-node/releases](https://github.com/neo-project/neo-node/releases). |
| neo-node README: `cd neo-node/neo-cli` | Path is `src/Neo.CLI` |
| neo-node README: “.NET Core”, Ubuntu 14/16/18 | Projects target **.NET 10** (`net10.0`) |
| `dotnet neo-cli.dll` | Published output may be `Neo.CLI.dll` / `neo-cli`; run from the publish folder |
| `list nativecontract` sample includes **NameService** | Not a native contract in current `neo` source. See [non-native-contracts](https://github.com/neo-project/non-native-contracts). |
| Root README “Documentation »” → docs.neo.org | N3 developer hub is [developers.neo.org](https://developers.neo.org/) |
| This git workspace containing `src/Neo.CLI` | Official **neo-project/neo** `master-n3` `src/` is `Neo`, `Neo.IO`, `Neo.Json`, `Neo.Extensions` only. Extra folders in a fork are local. |

## How we verified

- `gh api repos/neo-project/neo/contents/src?ref=master-n3`
- `gh api repos/neo-project/neo-node/contents/src?ref=master-n3` and `…/plugins`
- `gh api repos/neo-project/neo-vm/contents/src?ref=master`
- `src/Directory.Build.props` in this repo: `VersionPrefix` 3.10.2, tests `net10.0`
- neo-node and neo-vm csproj / Directory.Build.props: `net10.0`, `3.10.2`
- `src/Neo/SmartContract/Native/` file list for native contracts
- `src/Neo/Hardfork.cs` for hardfork names

When official pages and GitHub disagree, **these docs follow GitHub `master-n3` / neo-vm `master`**.
