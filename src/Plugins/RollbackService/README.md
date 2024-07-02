# RollbackPlugin for Neo

## Overview

The RollbackPlugin is designed for Neo nodes to provide a rollback mechanism, allowing the blockchain to revert to a specified block height. This is particularly useful for resynchronizing plugins that may have crashed or paused, ensuring they have up-to-date and consistent block execution information without requiring a full blockchain resynchronization.

## Features

- **Monitor and Record State Changes**: Records blockchain state changes during block commits.
- **Rollback Command**: Provides a command to rollback the blockchain to a specified height.
- **Reapply Blocks**: After rollback, reprocesses blocks to update plugin state.
- **Error Handling and Logging**: Robust error handling and detailed logging during rollback.

## Installation

   ```bash
   install RollbackServer
   ```

## Usage

### Rollback Command

Use the following command to rollback the blockchain to a specified block height:

```bash
rollback ledger <target_block_height>
```

For example, to rollback to block height 1000:

```bash
rollback ledger 1000
```

### Example

1. **Start Neo Node:**

   Start your Neo node as usual.

2. **Invoke Rollback Command:**

   ```bash
   fallback ledger 1000
   ```

   This will rollback the blockchain state to block height 1000 and reapply blocks from that height onward.

## Configuration

The plugin uses a configuration file located at `Plugins/RollbackService/config.json`. Ensure the configuration matches your Neo node settings.
