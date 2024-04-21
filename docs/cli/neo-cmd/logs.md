# Logs

## Running as Service

### Windows
Uses `Windows Event Log` otherwise uses console.

### Linux
Uses `syslog` otherwise uses console.

## Configuration
Reference [Log Levels](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-8.0#log-level)
Reference [Log Scopes](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#configure-logging-without-code)

## Scopes
- `Blockchain.Blocks.{hash}`
- `Blockchain.Transactions.{hash}`
- `Blockchain.Contracts.{hash}.Logs`
- `Blockchain.Contracts.{hash}.Events.{name}`

_Notes:_
1. _Replace `{hash}` with `ScriptHash`, `transaction hash` or `block hash` basic off it's category._
1. _Replace `{name}` with the event name of the contract._

## Example Filtering

### All Blockchain Log Data

```json
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Blockchain": "Trace"
    }
  },
```

**Above Includes**
- All `Runtime.Log`.
- All `Runtime.Notify` and contract events.
- All block data in `json` format.
- All transaction data in `json` format.

**Log Level**
- `Debug` - includes Notify events.
- `Trace` - includes Log events.

---

### All Contract Log Data

```json
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Blockchain.Contracts": "Trace"
    }
  },
```

**Above Includes**
- All `Runtime.Log`.
- All `Runtime.Notify` and contract events.

**Log Level**
- `Debug` - includes Notify events.
- `Trace` - includes Log events.

---

### All contracts transfer events.

```json
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Blockchain.Contracts.*.Events.Transfer": "Debug"
    }
  },
```

### Filter by Contract
This example we use the `GasToken` native contract; filtering by the `Transfer` event.

1. Add a new `json` section within `LogLevel` section with value `Blockchain.Contracts.{hash}.Events.{name}`.
1. We replaced `{hash}` with the `scripthash` of the contract; and `{name}` with the event name of the contract.
1. Set `json` string value to log level `Debug`.

### All events and logs.
```json
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Blockchain.Contracts.0xd2a4cff31913016155e38e474a2c06d08be276cf": "Debug"
    }
  },
```

### All events.
```json
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Blockchain.Contracts.0xd2a4cff31913016155e38e474a2c06d08be276cf.Events": "Debug"
    }
  },
```

### All transfers.
```json
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Blockchain.Contracts.0xd2a4cff31913016155e38e474a2c06d08be276cf.Events.Transfer": "Debug"
    }
  },
```

---
