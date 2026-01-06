<p align="center">
  <a href="https://neo.org/">
      <img
      src="https://neo3.azureedge.net/images/logo%20files-dark.svg"
      width="250px" alt="neo-logo">
  </a>
</p>

<h3 align="center">CSharp implementation of the neo blockchain protocol.</h3>

<p align="center">
   A modern distributed network for the Smart Economy.
  <br>
  <a href="https://docs.neo.org/"><strong>Documentation »</strong></a>
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
  <a href="https://discord.com/invite/rvZFQ5382k">
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
  <a href="https://deepwiki.com/neo-project/neo">
    <img src="https://deepwiki.com/badge.svg" alt="Ask DeepWiki.">
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

## Table of Contents
1. [Overview](#overview)
2. [Project structure](#project-structure)
3. [Related projects](#related-projects)
4. [Opening a new issue](#opening-a-new-issue)
5. [Contributing](#contributing)
6. [Bounty program](#bounty-program)
7. [License](#license)

## Overview
This repository is a csharp implementation of the [neo](https://neo.org) blockchain. It is jointly maintained by the neo core developers and neo global development community.
Visit the [tutorials](https://docs.neo.org) to get started.


## Project structure
An overview of the project folders can be seen below.

|Folder|Content|
|---|---|
|[/src/neo/Cryptography/](https://github.com/neo-project/neo/tree/master/src/Neo/Cryptography)|General cryptography implementation, including ECC.|
|[/src/neo/IO/](https://github.com/neo-project/neo/tree/master/src/Neo/IO)|Data structures used for caching and collection interaction.|
|[/src/neo/Ledger/](https://github.com/neo-project/neo/tree/master/src/Neo/Ledger)|Classes responsible for the state control, including the `MemoryPool` and `Blockchain`.|
|[/src/neo/Network/](https://github.com/neo-project/neo/tree/master/src/Neo/Network)|Peer-to-peer protocol implementation.|
|[/src/neo/Persistence/](https://github.com/neo-project/neo/tree/master/src/Neo/Persistence)|Classes used to allow other classes to access application state.|
|[/src/neo/Plugins/](https://github.com/neo-project/neo/tree/master/src/Neo/Plugins)|Interfaces used to extend Neo, including the storage interface.|
|[/src/neo/SmartContract/](https://github.com/neo-project/neo/tree/master/src/Neo/SmartContract)|Native contracts, `ApplicationEngine`, `InteropService` and other smart-contract related classes.|
|[/src/neo/Wallets/](https://github.com/neo-project/neo/tree/master/src/Neo/Wallets)|Wallet and account implementation.|
|[/src/Neo.Extensions/](https://github.com/neo-project/neo/tree/master/src/Neo.Extensions)| Extensions to expand the existing functionality.|
|[/src/Neo.Json/](https://github.com/neo-project/neo/tree/master/src/Neo.Json)| Neo's JSON specification.|
|[/tests/](https://github.com/neo-project/neo/tree/master/tests)|All unit tests.|

## Related projects
Code references are provided for all platform building blocks. That includes the base library, the VM, a command line application and the compiler.

* [neo:](https://github.com/neo-project/neo/) The core libraries for NEO.
* [neo-node:](https://github.com/neo-project/neo-node/) The `node-cli` implementation to start a NEO node, and Plugins(including DBFT, RpcServer, Oracle, ApplicationLogs, etc.).
* [neo-vm:](https://github.com/neo-project/neo-vm/) The Neo Virtual Machine implementation.
* [neo-express:](https://github.com/neo-project/neo-express/) A private net optimized for development scenarios.
* [neo-devpack-dotnet:](https://github.com/neo-project/neo-devpack-dotnet/) These are the official tools used to convert a C# smart-contract into a *neo executable file*.
* [neo-proposals:](https://github.com/neo-project/proposals) NEO Enhancement Proposals (NEPs) describe standards for the NEO platform, including core protocol specifications, client APIs, and contract standards.
* [neo-non-native-contracts:](https://github.com/neo-project/non-native-contracts) Includes non-native contracts that live on the blockchain, included but not limited to NeoNameService.

## Opening a new issue
Please feel free to create new issues to suggest features or ask questions.

- [Feature request](https://github.com/neo-project/neo/issues/new?assignees=&labels=discussion&template=feature-or-enhancement-request.md&title=)
- [Bug report](https://github.com/neo-project/neo/issues/new?assignees=&labels=&template=bug_report.md&title=)
- [Questions](https://github.com/neo-project/neo/issues/new?assignees=&labels=question&template=questions.md&title=)

If you found a security issue, please refer to our [security policy](https://github.com/neo-project/neo/security/policy).

## Contributing

We welcome contributions to the Neo project! To ensure a smooth collaboration process, please follow these guidelines:

### Branch Rules

- **`master`** - The NEO-4 development branch.
- **`master-n3`** - The NEO-3 development branch.

### Pull Request Guidelines

**Important**: All pull requests must be based on the `master` or/and `master-n3` branch.

1. **Fork the repository** and create your feature branch from `master` or/and `master-n3`:
   ```bash
   git checkout master-n3 # or git checkout master
   git pull origin master-n3 # or git pull origin master
   git checkout -b feature/your-feature-name # or bugfix/your-bugfix-name
   ```

2. **Make your changes** following the project's coding standards and conventions.

3. **Test your changes** thoroughly to ensure they don't break existing functionality.

4. **Commit your changes** with clear, descriptive commit messages:
   ```bash
   git commit -m "feature: add new feature description" # or bugfix: fix bug description
   ```

5. **Push to your fork** and create a pull request against the `master` or/and `master-n3` branch:
   ```bash
   git push origin feature/your-feature-name # or bugfix/your-bugfix-name
   ```

6. **Create a Pull Request** targeting the `master` or/and `master-n3` branch with:
   - Clear title and description
   - Reference to any related issues
   - Summary of changes made

### Development Workflow

```
feature, bugfix, etc. → master or/and master-n3 → release-vX.Y
hotfix → release-vX.Y
```

- Feature branches are merged into `master` or/and `master-n3`.
- Create a release branch from `master` or/and `master-n3` periodically.
- Only hotfix branches are merged into release branches.

For more detailed contribution guidelines, please check our documentation or reach out to the maintainers.

## Bounty program
You can be rewarded by finding security issues. Please refer to our [bounty program page](https://neo.org/bounty) for more information.

## License
The NEO project is licensed under the [MIT license](LICENSE).
