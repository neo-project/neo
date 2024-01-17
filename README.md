<p align="center">
  <a href="https://neo.org/">
      <img
      src="https://neo3.azureedge.net/images/logo%20files-dark.svg"
      width="250px" alt="neo-logo">
  </a>
</p>

<h3 align="center">Neo Blockchain</h3>

<p align="center">
   A modern distributed network for the Smart Economy.
  <br>
  <a href="https://docs.neo.org/docs/en-us/index.html"><strong>Documentation »</strong></a>
  <br>
  <br>
  <a href="https://github.com/neo-project/neo"><strong>Neo</strong></a>
  ·
  <a href="https://github.com/neo-project/neo-modules">Neo Modules</a>
  ·
  <a href="https://github.com/neo-project/neo-devpack-dotnet">Neo DevPack</a>
</p>
<p align="center">
  <a href="https://twitter.com/neo_blockchain">
      <img
      src=".github/images/twitter-logo.png"
      width="25px">
  </a>
  &nbsp;
  <a href="https://medium.com/neo-smart-economy">
      <img
      src=".github/images/medium-logo.png"
      width="23px">
  </a>
  &nbsp;
  <a href="https://neonewstoday.com">
      <img
      src=".github/images/nnt-logo.jpg"
      width="23px">
  </a>
  &nbsp;
  <a href="https://t.me/NEO_EN">
      <img
      src=".github/images/telegram-logo.png"
      width="24px" >
  </a>
  &nbsp;
  <a href="https://www.reddit.com/r/NEO/">
      <img
      src=".github/images/reddit-logo.png"
      width="24px">
  </a>
  &nbsp;
  <a href="https://discord.io/neo">
      <img
      src=".github/images/discord-logo.png"
      width="25px">
  </a>
  &nbsp;
  <a href="https://www.youtube.com/neosmarteconomy">
      <img
      src=".github/images/youtube-logo.png"
      width="32px">
  </a>
  &nbsp;
  <!--How to get a link? -->
  <a href="https://neo.org/">
      <img
      src=".github/images/we-chat-logo.png"
      width="25px">
  </a>
  &nbsp;
  <a href="https://weibo.com/neosmarteconomy">
      <img
      src=".github/images/weibo-logo.png"
      width="28px">
  </a>
</p>
<p align="center">
  <a href="https://github.com/neo-project/neo/releases">
    <img src="https://badge.fury.io/gh/neo-project%2Fneo.svg" alt="Current neo version.">
  </a>
  <a href='https://coveralls.io/github/neo-project/neo'>
    <img src='https://coveralls.io/repos/github/neo-project/neo/badge.svg' alt='Coverage Status' />
  </a>
  <a href="https://github.com/neo-project/neo/blob/master/LICENSE">
    <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License.">
  </a>
</p>

<p align="center">
  <a href="https://codespaces.new/neo-project/neo">
    <img src="https://github.com/codespaces/badge.svg" alt="Open in GitHub Codespaces.">
  </a>
</p>

## Overview
This repository contain main classes of the
[Neo](https://www.neo.org) blockchain.
Visit the [documentation](https://docs.neo.org/docs/en-us/index.html) to get started.

## Project structure
An overview of the project folders can be seen below.

| Folder        | Content                                                                                           |
|---------------|---------------------------------------------------------------------------------------------------|
| Consensus     | Classes used in the dBFT consensus algorithm, including the `ConsensusService` actor.             |
| Cryptography  | General cryptography classes including ECC implementation.                                        |
| IO            | Data structures used for caching and collection interaction.                                      |
| Ledger        | Classes responsible for the state control, including the `MemoryPool` and `Blockchain` classes.   |
| Network       | Peer-to-peer protocol implementation classes.                                                     |
| Persistence   | Classes used to allow other classes to access application state.                                  |
| Plugins       | Interfaces used to extend Neo, including the storage interface.                                   |
| SmartContract | Native contracts, `ApplicationEngine`, `InteropService` and other smart-contract related classes. |
| VM            | Helper methods used to interact with the VM.                                                      |
| Wallet        | Wallet and account implementation.                                                                |

## Downloads

Below are the available downloads for our project. Please select the appropriate file for your needs.

### NEO CLI Node
| Platform    | File                    | SHA256                                                           | Download                                                                                           |
|-------------|-------------------------|------------------------------------------------------------------|----------------------------------------------------------------------------------------------------|
| Linux       | neo-cli-linux-x64.zip   | 8195918E942D2B3BF5BD0ACC8A29157197F4594EC484E1AAC719E6605E4EF299 | [V3.6.2](https://github.com/neo-project/neo-node/releases/download/v3.6.2/neo-cli-linux-x64.zip)   |
| Windows     | neo-cli-win-x64.zip     | 4CFDD39DD7C0D37940BBBC4D42403599FFF96D4A8F9A8D394BB9A07400CAB47D | [V3.6.2](https://github.com/neo-project/neo-node/releases/download/v3.6.2/neo-cli-win-x64.zip)     |
| Mac OS      | neo-cli-osx-x64.zip     | D0A5A50751353E22886C11EE4F8AE6C3DDBDE7EA090FE7B58A3064BAB3C8950D | [V3.6.2](https://github.com/neo-project/neo-node/releases/download/v3.6.2/neo-cli-osx-x64.zip)     |
| Mac OS ARM  | neo-cli-osx-arm-x64.zip | CAC83365F9E9E24F20BEB98C1BB9AD1FD9240E0A28F1D5DDDE0C58E5AA8D49F3 | [V3.6.2](https://github.com/neo-project/neo-node/releases/download/v3.6.2/neo-cli-osx-arm-x64.zip) |
| Portable    | neo-cli-portable.zip    | D268777F2A6C7E801E8FC3856BE72D336E0E2B6623BD76F2F8816D3A0F00D1D8 | [V3.6.2](https://github.com/neo-project/neo-node/releases/download/v3.6.2/neo-cli-portable.zip)    |

### NEO Modules

| File                | Description                                                                 | Download                                                                                           |
|---------------------|-----------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------|
| ApplicationLogs.zip | Synchronizes the smart contract log with the NativeContract log (Notify)    | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/ApplicationLogs.zip)  |
| DBFTPlugin.zip      | Consensus plugin with dBFT algorithm.                                       | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/DBFTPlugin.zip)       |
| LevelDBStore.zip    | Uses LevelDB to store the blockchain data                                   | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/LevelDBStore.zip)     |
| MPTTrie.zip         | 11.8 KB                                                                     | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/MPTTrie.zip)          |
| OracleService.zip   | Built-in oracle plugin                                                      | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/OracleService.zip)    |
| RocksDBStore.zip    | Uses RocksDBStore to store the blockchain data                              | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/RocksDBStore.zip)     |
| RpcClient.zip       | 53.9 KB                                                                     | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/RpcClient.zip)        |
| RpcServer.zip       | Enables RPC for the node                                                    | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/ApplicationLogs.zip)  |
| SQLiteWallet.zip    | A SQLite-based wallet provider that supports wallet files with .db3 suffix. | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/SQLiteWallet.zip)     |
| StatesDumper.zip    | Exports Neo-CLI status data                                                 | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/StatesDumper.zip)     |
| StateService.zip    | Enables MPT for the node                                                    | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/StateService.zip)     |
| StorageDumper.zip   | Exports Neo-CLI status data                                                 | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/StorageDumper.zip)    |
| TokensTracker.zip   | Enquiries balances and transaction history of accounts through RPC          | [V3.6.3](https://github.com/neo-project/neo-modules/releases/download/v3.6.3/TokensTracker.zip)    |

## Related projects
Code references are provided for all platform building blocks. That includes the base library, the VM, a command line application and the compiler.

* [neo:](https://github.com/neo-project/neo/) Neo core library, contains base classes, including ledger, p2p and IO modules.
* [neo-modules:](https://github.com/neo-project/neo-modules/) Neo modules include additional tools and plugins to be used with Neo.
* [neo-devpack-dotnet:](https://github.com/neo-project/neo-devpack-dotnet/) These are the official tools used to convert a C# smart-contract into a *neo executable file*.

## Opening a new issue
Please feel free to create new issues to suggest features or ask questions.

- [Feature request](https://github.com/neo-project/neo/issues/new?assignees=&labels=discussion&template=feature-or-enhancement-request.md&title=)
- [Bug report](https://github.com/neo-project/neo/issues/new?assignees=&labels=&template=bug_report.md&title=)
- [Questions](https://github.com/neo-project/neo/issues/new?assignees=&labels=question&template=questions.md&title=)

If you found a security issue, please refer to our [security policy](https://github.com/neo-project/neo/security/policy).

## Bounty program
You can be rewarded by finding security issues. Please refer to our [bounty program page](https://neo.org/bounty) for more information.

## License
The NEO project is licensed under the [MIT license](LICENSE).
