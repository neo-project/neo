# README for Application and Protocol Configuration JSON File

This README provides an explanation for each field in the JSON configuration file for a Neo node.

## ApplicationConfiguration

### Logger
- **Path**: Directory where log files are stored. Default is "Logs".
- **ConsoleOutput**: Boolean flag to enable or disable console output for logging. Default is `false`.
- **Active**: Boolean flag to activate or deactivate the logger. Default is `false`.

### Storage
- **Engine**: Specifies the storage engine used by the node. Possible values are:
    - `MemoryStore`
    - `LevelDBStore`
    - `RocksDBStore`
- **Path**: Path to the data storage directory. `{0}` is a placeholder for the network ID.

### P2P
- **Port**: Port number for the P2P network. MainNet is `10333`, TestNet is `20333`.
- **MinDesiredConnections**: Minimum number of desired P2P connections. Default is `10`.
- **MaxConnections**: Maximum number of P2P connections. Default is `40`.
- **MaxConnectionsPerAddress**: Maximum number of connections allowed per address. Default is `3`.

### UnlockWallet
- **Path**: Path to the wallet file.
- **Password**: Password for the wallet.
- **IsActive**: Boolean flag to activate or deactivate the wallet. Default is `false`.

### Contracts
- **NeoNameService**: Script hash of the Neo Name Service contract. MainNet is `0x50ac1c37690cc2cfc594472833cf57505d5f46de`, TestNet is `0x50ac1c37690cc2cfc594472833cf57505d5f46de`.

### Plugins
- **DownloadUrl**: URL to download plugins, typically from the Neo project's GitHub releases. Default is `https://api.github.com/repos/neo-project/neo/releases`.
- **CustomUrls**: List of custom URLs for downloading plugins.

<div style="border: 1px solid #f8d7da; background-color: #f8d7da; color: #721c24; padding: 10px; border-radius: 5px;">
  <strong>Warning:</strong> Plugin from the `DownloadUrl` will be installed if different plugins with the same name exist in different urls.
</div>

## ProtocolConfiguration

### Network
- **Network**: Network ID for the Neo network. MainNet is `860833102`, TestNet is `894710606`

### AddressVersion
- **AddressVersion**: Version byte used in Neo address generation. Default is `53`.

### MillisecondsPerBlock
- **MillisecondsPerBlock**: Time interval between blocks in milliseconds. Default is `15000` (15 seconds).

### MaxTransactionsPerBlock
- **MaxTransactionsPerBlock**: Maximum number of transactions allowed per block. Default is `512`.

### MemoryPoolMaxTransactions
- **MemoryPoolMaxTransactions**: Maximum number of transactions that can be held in the memory pool. Default is `50000`.

### MaxTraceableBlocks
- **MaxTraceableBlocks**: Maximum number of blocks that can be traced back. Default is `2102400`.

### Hardforks
- **HF_Aspidochelone**: Block height for the Aspidochelone hard fork. MainNet is `1730000`, TestNet is `210000`.
- **HF_Basilisk**: Block height for the Basilisk hard fork. MainNet is `4120000`, TestNet is `2680000`.
- **HF_Cockatrice**: Block height for the Cockatrice hard fork. MainNet is `5450000`, TestNet is `3967000`.

### InitialGasDistribution
- **InitialGasDistribution**: Total amount of GAS distributed initially. Default is `5,200,000,000,000,000 Datoshi` (`52,000,000 GAS`).

### ValidatorsCount
- **ValidatorsCount**: Number of consensus validators. Default is `7`.

### StandbyCommittee
- **StandbyCommittee**: List of public keys for the standby committee members.

### SeedList
- **SeedList**: List of seed nodes with their addresses and ports.
  - MainNet addresses are:
      - `seed1.neo.org:10333`
      - `seed2.neo.org:10333`
      - `seed3.neo.org:10333`
      - `seed4.neo.org:10333`
      - `seed5.neo.org:10333`
  - TestNet addresses are:
      - `seed1t5.neo.org:20333`
      - `seed2t5.neo.org:20333`
      - `seed3t5.neo.org:20333`
      - `seed4t5.neo.org:20333`
      - `seed5t5.neo.org:20333`

This configuration file is essential for setting up and running a Neo node, ensuring proper logging, storage, network connectivity, and consensus protocol parameters.
