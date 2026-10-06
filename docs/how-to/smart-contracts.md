# How to: smart contracts (C# stack)

Contracts are **not** written inside `neo-project/neo`. You write C# (or another supported language), compile to **NEF + manifest**, then deploy with Neo-CLI or an SDK.

## Pieces

| Piece | Repository | Role |
| --- | --- | --- |
| Compiler / devpack | [neo-devpack-dotnet](https://github.com/neo-project/neo-devpack-dotnet) | C# → NeoVM bytecode (`.nef`) + `manifest.json` |
| VM | [neo-vm](https://github.com/neo-project/neo-vm) | Runs opcodes |
| Host | [neo](https://github.com/neo-project/neo) (`ApplicationEngine`) | Syscalls, storage, fees |
| Node | [neo-node](https://github.com/neo-project/neo-node) | `deploy` / `invoke` commands, RPC |

Official deploy guide: [Deploying smart contracts](https://developers.neo.org/docs/n3/develop/deploy/deploy). Interop list: [Smart contract API](https://developers.neo.org/docs/n3/reference/scapi/interop).

## Mental model

1. You write a class with methods the manifest will export.
2. The compiler emits a **NEF** (bytecode) and a **manifest** (methods, permissions, events).
3. `deploy` creates a transaction to `ContractManagement`.
4. Later `invoke` builds a script that `CALLT`s your method.
5. Every node runs the same script in NeoVM. If your code uses `DateTime.Now` or random without the chain’s `Runtime.GetRandom`, nodes would disagree — the compiler and runtime forbid that class of mistake.

## Local loop (beginner)

1. Install the SDK / Neo Blockchain Toolkit, or follow current neo-devpack docs.
2. Compile → `YourContract.nef` + `YourContract.manifest.json`.
3. Run [Neo-CLI](run-a-node.md) on TestNet or a private net.
4. Open a wallet that has GAS (deployment costs system fee).
5. Deploy:

```text
neo> open wallet wallet.json
neo> deploy YourContract.nef YourContract.manifest.json
```

6. Invoke (example):

```text
neo> invoke 0x<scripthash> symbol
```

`VM State: HALT` means success. `FAULT` means the engine aborted (exception, missing witness, out of GAS).

Dry-run RPC `invokefunction` does **not** persist storage; only a relayed transaction does.

## Native contracts you can call

You do not deploy these; they already exist. From CLI: `list nativecontract`.

Typical hashes (N3, same on MainNet/TestNet for natives):

| Name | Hash (from current CLI docs / source) |
| --- | --- |
| ContractManagement | `0xfffdc93764dbaddd97c48f252a53ea4643faa3fd` |
| NeoToken | `0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5` |
| GasToken | `0xd2a4cff31913016155e38e474a2c06d08be276cf` |
| PolicyContract | `0xcc5e4edd9f5f8dba8bb65734541df7a1c081c67b` |
| StdLib | `0xacce6fd80d44e1796aa0c2c625e9e4e0ce39efc0` |
| OracleContract | `0xfe924b7cfe89ddd271abaf7210a80a7e11178758` |

If a tutorial lists **NameService** as native, see [official docs notes](../reference/official-docs.md).

## When you must change *this* repo

Only if you are changing **protocol** behavior: a new syscall, a native method, a hardfork, or fee rules. Application logic stays in your contract project.

## Next

- [NeoVM](../neo-vm.md)
- [Architecture](../architecture.md)
- Examples: [developers.neo.org tutorials](https://developers.neo.org/tutorials)
