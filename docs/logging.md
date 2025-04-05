# Logging System

This document describes the logging system used in the `neo-cli` application.

## Overview

The node utilizes the **Serilog** library for flexible and structured logging. This replaces the previous custom logging implementation.

## Configuration

Logging is configured programmatically at startup within `src/Neo.CLI/Program.cs`. Configuration is **not** loaded from `config.json`.

## Sinks (Outputs)

By default, logs are written to two sinks:

1.  **Console:**
    *   Outputs messages with a severity level of `Information` or higher.
    *   Format: `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}`
    *   Example: `[14:32:05 INF] Starting Neo CLI with Minimum Log Level: Information`

2.  **File:**
    *   Outputs messages based on the configured minimum log level (see below).
    *   Location: `Logs/neo-node-.log` (relative to the execution directory).
    *   Rotation: Creates a new file daily (`RollingInterval.Day`).
    *   Retention: Keeps logs for the last 7 days (`retainedFileCountLimit: 7`).
    *   Buffered: Uses buffered writing for better performance.
    *   Format: `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}`
    *   Example: `2023-10-27 14:32:05.123 +01:00 [INF] (Neo.CLI.Program) Starting Neo CLI with Minimum Log Level: Information`

## Log Levels

The logging system supports the standard Serilog levels (in order of increasing severity):

*   `Verbose`
*   `Debug`
*   `Information`
*   `Warning`
*   `Error`
*   `Fatal`

## Controlling Log Level via Command Line

You can control the minimum level of messages captured **globally** (and thus sent to the File sink) using a command-line argument when starting `neo-cli`:

```bash
neo-cli.exe --loglevel <level>
# or
neo-cli.exe -l <level>
```

Where `<level>` can be one of the log level names (case-insensitive), e.g.:

*   `neo-cli.exe --loglevel Debug` (Logs Debug, Information, Warning, Error, Fatal to file)
*   `neo-cli.exe -l Verbose` (Logs everything to file)
*   `neo-cli.exe --loglevel Warning` (Logs Warning, Error, Fatal to file)

**Default:** If the `--loglevel` argument is omitted or invalid, the minimum level defaults to `Information`.

**Note:** The Console sink remains restricted to `Information` and above, unless the global minimum level is set even higher (e.g., `--loglevel Warning` would make the console show Warning+).

## Structured Logging

The logging system emphasizes structured logging. Instead of just logging text messages, messages are treated as templates with named placeholders. This allows for easier querying and analysis of log data.

**Example Code:**

```csharp
_log.Information("Processed block {BlockIndex} in {DurationMs} ms", block.Index, stopwatch.ElapsedMilliseconds);
```

This logs the `BlockIndex` and `DurationMs` as distinct properties alongside the message template, which is more powerful than simple string formatting.

## Enrichment

Logs are automatically enriched with:

*   `SourceContext`: The name of the class where the log originated.
*   `ThreadId`: The managed thread ID.
