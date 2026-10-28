# How to: build and test

Covers all three official repositories. Use **.NET 10**.

## Check the SDK

```bash
dotnet --version
```

You want a **10.x** SDK. The projects set `<TargetFramework>net10.0</TargetFramework>` (neo, neo-node, neo-vm).

## 1. Protocol library — `neo-project/neo`

```bash
git clone https://github.com/neo-project/neo.git
cd neo
git checkout master-n3
```

Restore and test:

```bash
dotnet test tests/Neo.UnitTests/Neo.UnitTests.csproj
```

Useful filters:

```bash
dotnet test tests/Neo.UnitTests/Neo.UnitTests.csproj --filter "FullyQualifiedName~UT_StdLib"
```

Other test projects live under `tests/` (`Neo.Json.UnitTests`, `Neo.Extensions.Tests`, …).

Open `neo.sln` in your IDE. TreatWarningsAsErrors is on; fix warnings, do not suppress them without a reason.

### Which branch?

| Work | Branch |
| --- | --- |
| N3 protocol, current mainnet rules | `master-n3` |
| NEO-4 | `master` |

Do not open an N3 bugfix against `master`.

## 2. Node — `neo-project/neo-node`

```bash
git clone https://github.com/neo-project/neo-node.git
cd neo-node
git checkout master-n3
dotnet test
```

Publish CLI:

```bash
dotnet publish src/Neo.CLI -c Release -o ./publish
```

Then [run a node](run-a-node.md) from `./publish`.

The neo-node README still shows `cd neo-node/neo-cli`. The project path is **`src/Neo.CLI`**.

## 3. VM — `neo-project/neo-vm`

```bash
git clone https://github.com/neo-project/neo-vm.git
cd neo-vm
git checkout master
dotnet test tests/Neo.VM.Tests
```

## Working on a feature that spans repos

Example: a new opcode that native contracts will use.

1. Implement and test in **neo-vm**.
2. Bump / ProjectReference the VM in **neo**, add host tests (fees, syscalls).
3. If CLI must expose it, change **neo-node** last.

Core-dev practice is to land VM then library then node, so binaries stay consistent.

## If tests fail on your machine

- Confirm `net10.0` is installed: `dotnet --list-sdks`
- On this repo, tests are **not parallel** by default (`DoNotParallelize` unless a project opts in)
- Native store tests need the right native DLL on Windows (LevelDB from neo-project)

## Next

- [Run a node](run-a-node.md)
- [Contribute](contribute.md)
- [Neo core](../neo-core.md)
