# LedgerDebugger Plugin

[![Version](https://img.shields.io/badge/Version-1.0.0-blue.svg)](https://github.com/neo-project/neo-plugins)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/neo-project/neo-plugins/blob/master/LICENSE)

## Overview

The LedgerDebugger plugin provides functionality to capture and replay blockchain blocks without requiring a full ledger sync. It records the minimal state needed during block execution (the "read set"), allowing blocks to be reproduced later for debugging or analysis.

## Features

- 📝 **State Capture**: Records the exact storage state read during block execution
- 🔄 **Block Replay**: Re-execute blocks with captured state without full ledger sync
- 📊 **Content-Addressable Storage**: Optimizes storage by deduplicating identical values
- 🔍 **Transaction Debugging**: Isolate and debug specific transactions within a block
- 🧩 **Minimal State Storage**: Only records state actually used during execution

## Installation

### Prerequisites

- [Neo Node](https://github.com/neo-project/neo-node) latest version
- [.NET SDK](https://dotnet.microsoft.com/download) 9.0 or later

### Setup

1. Build the plugin or download a release:
   ```bash
   cd neo-plugins
   dotnet build /p:Configuration=Release
   ```

2. Copy the plugin files to your Neo Node's `Plugins` directory:
   ```bash
   cp src/Plugins/LedgerDebugger/bin/Release/net9.0/* /path/to/neo-node/Plugins/
   ```

3. Update your Neo Node configuration to include the plugin.

## Configuration

The plugin is configured through the `LedgerDebugger.json` file:

```json
{
  "PluginConfiguration": {
    "Path": "ReadSets_{0}",
    "StoreProvider": "LevelDBStore",
    "MaxReadSetsToKeep": 10000
  }
}
```

| Option | Description | Default Value |
|--------|-------------|---------------|
| `Path` | Storage path pattern. `{0}` is replaced with network identifier | `ReadSets_{0}` |
| `StoreProvider` | Storage provider type (e.g., `LevelDBStore`, `RocksDBStore`, `MemoryStore`) | `LevelDBStore` |
| `MaxReadSetsToKeep` | Maximum number of read sets to keep before cleanup | `10000` |

## Technical Details

### How It Works

1. **Capturing Phase**:
   - During normal blockchain operation, the plugin intercepts all storage reads
   - It records the values read from storage during block execution
   - This read set represents the minimal state needed to re-execute the block

2. **Storage Optimization**:
   - Using content-addressable storage, large values (>32 bytes) are stored by their hash
   - Small values (≤32 bytes) are stored directly
   - This enables automatic deduplication of identical data across blocks

3. **Replaying Phase**:
   - When a block needs to be replayed, its read set is loaded
   - A memory store is populated with only the required state
   - The block is executed against this minimal state snapshot

### Storage Efficiency

The content-addressable storage approach provides significant benefits:

- **Deduplication**: Identical values are stored only once, regardless of how many times they appear
- **Size Optimization**: Small values bypass the hashing overhead
- **Minimal Footprint**: Only stores state that's actually read during execution
- **Scalability**: Storage requirements grow proportionally to unique state values, not block count

## Best Practices

- Regularly prune old read sets if storage space is constrained
- For debug scenarios, focus on specific transactions to reduce noise
- When debugging contracts, capture blocks that interact with the contract
- Use alongside other debugging tools for comprehensive analysis

## Limitations

- Only captures state read during regular execution, not during verification
- Does not record network state or peer communications
- Requires the original block to be available in the chain

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
