# NeoVM (`neo-project/neo-vm`)

NeoVM is a small **stack machine** that runs contract bytecode. It is published as NuGet package **`Neo.VM`**.

- Source: [github.com/neo-project/neo-vm](https://github.com/neo-project/neo-vm)
- Default branch: **`master`** (there is no `master-n3` on this repo)
- Target: **net10.0**, version prefix **3.10.2**
- Official concept page (accurate): [NeoVM](https://developers.neo.org/docs/n3/foundation/neovm)

This guide lives in **neo-project/neo** so VM, host, and node are documented together. Opcode PRs still go to **neo-vm**.

## What NeoVM is *not*

- It does **not** store blocks.
- It does **not** implement `System.Storage.*` or `CheckWitness`.
- It does **not** charge GAS by itself.

Those belong to **`ApplicationEngine`** in [neo-core](neo-core.md). The VM calls out through an interop/syscall interface; the host decides what each syscall does and how much it costs.

## Pieces (from the official page + source)

| Piece | Role |
| --- | --- |
| **ExecutionEngine** | Fetches the next instruction and runs it |
| **Invocation stack** | Nested call frames (`CALL`, `CALLT`, contract calls) |
| **Evaluation stack** | Data the current instruction reads/writes |
| **Result stack** | Values left when the script finishes (`HALT`) |
| **Jump table** | Maps opcodes to C# handlers (hardforks can swap tables) |

Execution:

1. Compiler (for example [neo-devpack-dotnet](https://github.com/neo-project/neo-devpack-dotnet)) emits a **NEF** (NeoVM bytecode).
2. Host creates an engine, loads the script, pushes a context.
3. Loop: read opcode → handler → maybe syscall into the host.
4. `RET`/`ABORT` or empty instruction pointer → `HALT` or `FAULT`.

## Use it from another app

```bash
dotnet add package Neo.VM
```

You can embed the VM in tests or non-chain tools. For chain execution always use `ApplicationEngine` so fees and storage match consensus.

## Layout

```
neo-vm/
  src/Neo.VM/          # engine, instructions, types
  tests/Neo.VM.Tests/  # opcode tests
  benchmarks/
  neo-vm.sln
```

## Build and test

```bash
git clone https://github.com/neo-project/neo-vm.git
cd neo-vm
git checkout master
dotnet test tests/Neo.VM.Tests
```

When you change an opcode, add a test that runs a tiny script and checks the stack and `VMState` (`HALT` vs `FAULT`).

## How this repo uses NeoVM

In **neo**:

- `ApplicationEngine` subclasses/hosts the VM
- Opcode **prices** and some jump tables live next to the engine
- Interop methods are registered as syscalls

A hardfork that changes instruction *semantics* needs a **neo-vm** change. A hardfork that only changes *price* or which jump table the host selects may be **neo**-only.

## Related

- [Architecture](architecture.md)
- [How to: smart contracts](how-to/smart-contracts.md)
- [How to: build and test](how-to/build-and-test.md)
