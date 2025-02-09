# Error Codes
are used when an `Exception` is thrown, to identify known issues with
building, deploying or testing project's `*.nbproj` file or `MSBuild` tasks
within `Visual Studio` or `dotnet` CLI.

# How to use Error Codes
When an `Exception` occurs (_whether that be known or unknown_) the number
**MUST BE** set to the exception's `HResult` property. Doing so, will set the
exit code of the application. Exit codes are used by many applications within
the Neo Build Engine process. These codes are also used as an error string ID
for exception `Message` property, to help the user identify the root of the
problem. Error string will be prefixed with `NB` and have a suffix format of
`{0:d04}`, for _**example**: `NB1000`, `NB0101`, `NB0070`_. This allows
padding of zeros for anything less than `1000`.


# Module Filename Format
will be defined as `NeoBuildErrorCodes.{module}.cs` within `Neo.Build.Core`
library. For _**example**: `NeoBuildErrorCodes.Wallet.cs`_ file would hold
all the error codes for the wallet module. _See
[What is a Module](#what-is-a-module) for more details_.

# What is a Module
refers to the part of code doing the processing within the Neo Build Engine
for _**example**, `NeoBuildEngine::Wallet:Open()`_. `Wallet` is the module.
Each class **SHOULD BE** considered it's own module. Each subclass in the
module class that has `private` or `internal` **WILL NOT** be considered a
different module for _**example**, `NeoBuildEngine::Wallet::Cache:AddItem()`_.

# How to Define Module Base
Each module within Neo Build Engine has it own base error number. These
modules can hold up to `1000` known errors each. The formula for calculating
an error module's base is `mb = b * (n + 1)`; build engine's module base
number (_`b`_) times number (_`n`_) of modules plus one (_`1`_) equals module
base (_`mb`_).

## Factors
1. Module base _**MUST BE**_ unique.
1. Modules can _**Only**_ define exactly _**1000**_ known error codes.
1. Module **MUST** set `HResult` in `System.Exception` class.
1. Module **MUST** define the root cause of the error, setting `Message` in `System.Exception` class.

## Example
```csharp
// Base
private const int EngineBase = 1;

// Module 1
private const int ModuleBase = Base * 1000;     // 1000
public const int Exception1 = ModuleBase + 1;   // 1001
public const int Exception2 = ModuleBase + 2;   // 1002

// Module 2
private const int ModuleBase = Base * 2000;     // 2000
public const int Exception1 = ModuleBase + 1;   // 2001
public const int Exception2 = ModuleBase + 2;   // 2002
```
