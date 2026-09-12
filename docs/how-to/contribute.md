# How to: contribute

Rules below match [CONTRIBUTING.md](../../CONTRIBUTING.md) and the root [README](../../README.md#contributing).

## Pick the right repository

| Change | Open the PR on |
| --- | --- |
| Native contracts, ledger, P2P, `ApplicationEngine`, `DataCache` | **[neo-project/neo](https://github.com/neo-project/neo)** (this repo) |
| Neo-CLI commands, plugins, `config.json` defaults | **[neo-project/neo-node](https://github.com/neo-project/neo-node)** |
| Opcodes, `ExecutionEngine`, VM types | **[neo-project/neo-vm](https://github.com/neo-project/neo-vm)** |

A feature that needs all three should be described in one issue and implemented as **stacked PRs** (VM → neo → neo-node).

## Branch names

On **neo** and **neo-node**:

- `master-n3` — N3 (current public networks)
- `master` — NEO-4 (neo only; neo-node also uses `master-n3` for N3)

On **neo-vm**:

- `master` — current VM

## Workflow (this repo)

```bash
git clone https://github.com/<you>/neo.git
cd neo
git remote add neo https://github.com/neo-project/neo.git
git fetch neo
git checkout -b docs/your-topic neo/master-n3   # or feature/… from master-n3
```

1. Discuss non-trivial features in an issue first (`discussion` / `design` labels).
2. Add unit tests (`tests/Neo.UnitTests`).
3. Wait for **two** approvals on N3; leave **24 hours** after the last approval when you can.
4. Do not merge your own PR unless you are a maintainer following those rules.

Beginner-friendly issues: cosmetic and house-keeping labels (see CONTRIBUTING.md).

## Coding habits

- `TreatWarningsAsErrors` is on
- Prefer existing test patterns (`TestProtocolSettings.Default`, `TestBlockchain.GetTestSnapshotCache()`)
- Hardfork-gate consensus changes
- Do not add secrets or real private keys to tests

## Security

Report vulnerabilities via the [security policy](https://github.com/neo-project/neo/security/policy), not a public issue. Bounty: [neo.org/bounty](https://neo.org/bounty).

## Questions

GitHub issues are for bugs and features. Chat: [Discord](https://discord.com/invite/rvZFQ5382k).
