
# Contributing to NEO
Neo is an open-source project and it depends on its contributors and constant community feedback to implement the features required for an smart economy.  

You are more than welcome to join us in the development of Neo.  

Please read this whole document in order to understand how issues are organized and how you can start contributing.

*This document covers this repository only and does not include community repositories or repositories managed by NGD Shanghai and NGD Seattle.*

### Questions and Support
The issue list is reserved exclusively for bug reports and features discussions. If you have questions or need support, please visit us in our [Discord](https://discord.io/neo) server.  

### dApp Development
This document does not relate to dApp development. If you are looking to build a dApp using Neo, please [start here](https://neo.org/dev).

### Developer Guidance
We try to have as few rules as possible, just enough to keep the project organized. If you want to suggest any changes, please create an issue or message us on Discord.

##### Use complete titles
Use a comprehensive title and descriptions in your PRs. This is important because this information is used in our monthly development report.

##### Discuss before coding
Proposals must be discussed before being implemented. Create a new issue if you have an idea of a new feature or enhancement. Avoid implementing issues with the ![](./.github/images/discussion.png) tag.

##### Tests during code review
We expect reviewers to test the issue before approving or requesting changes.

##### Give time to other developers review an issue
Even if the code has been approved, you should leave at least 24 hours for others to review it before merging the code.

##### Read the PR template
Make sure you follow the instructions in our PR template to ensure your code is not rejected.

##### Create unit tests
Since all issues must be tested by the reviewers, it is important that the developer includes basic unit tests.

##### Task assignment
If a developer wants to work in a specific issue, he may ask the team to assign it to himself. The proposer of an issue has priority in task assignment.


### Issues for begginners
If you are looking to start contributing to NEO, we suggest you start working on issues with ![](./.github/images/cosmetic.png) or ![](./.github/images/house-keeping.png) tags, since they usually do not depend on extensive NEO platform knowledge. 

### Issues States
Usually, issues follow the ![](./.github/images/discussion.png) ![](./.github/images/solution-design-2.png) ![](./.github/images/ready-to-implement.png) ![](./.github/images/to-review.png)  flow, but this is not strictly mandatory.

#### Initial state: ![](./.github/images/discussion.png)
Whenever someone posts a new feature request, it is tagged with ![](./.github/images/discussion.png). This means that there is no consensus if the feature should be implemented or not. Avoid creating PR to solve issues in this state since it may be completely discarded.

#### Issue state: ![](./.github/images/solution-design-2.png)
When a feature request is accepted by the team, but there is no consensus about the implementation, the issue is tagged with ![](./.github/images/solution-design-2.png). We recommend the team to agree in the solution design before anyone attempts to implement it, using text or UML. It is not recommended, but developers can also present their solution using code.  
Note that PRs for issues in this state may also be discarded if the team disagree with the proposed solution.

#### Issue state: ![](./.github/images/ready-to-implement.png)
Once the team has agreed on feature and the proposed solution, the issue is tagged with ![](./.github/images/ready-to-implement.png). When implementing it, please follow the solution accepted by the team.

#### Issue state: ![](./.github/images/to-review.png)
Some issues need to be reviewed by the team. This tag may be used in issues that need attention or to create task lists.

### Issue Types

#### Type: ![](./.github/images/cosmetic.png)

Issues with the ![](./.github/images/cosmetic.png) tag are usually changes in code or documentation that improve user experience without affecting current functionality. These issues are recommended for begginners because they require little to no knowledge about Neo platform.

#### Type: ![](./.github/images/enhancement.png)

Enhancements are platform changes that may affect performance, usability or add new features to existing modules. It is recommended that developers have previous knowledge in the platform to work in these improvements, specially in more complicated modules like the ![](./.github/images/compiler.png), ![](./.github/images/ledger.png) and ![](./.github/images/consensus.png).

#### Type: ![](./.github/images/new-feature.png)

New features may include large changes in the code base. Some are complex, but some are not. So, a few issues with ![](./.github/images/new-feature.png) may be recommended for starters, specially those related to the ![](./.github/images/rpc.png) and the ![](./.github/images/sdk.png) module.

#### Type: ![](./.github/images/migration.png)

Issues related to the migration from Neo 2 to Neo 3 are tagged with ![](./.github/images/migration.png). These issues are usually the most complicated ones, since they require a deep knowledge in both versions.

### Tags for Project Modules 
These tags do not necessarily represent each module at code level. Modules ![](./.github/images/consensus.png) and ![](./.github/images/compiler.png) are not recommended for begginers.

#### Module: ![](./.github/images/compiler.png)
Issues that are related or influence the behavior of our C# compiler. Note that the compiler itself is hosted in the [neo-devpack-dotnet](https://github.com/neo-project/neo-devpack-dotnet) repository.

#### Module: ![](./.github/images/consensus.png)
Changes to consensus are usually harder to make and test. Avoid implementing issues in this module that are not yet decided.

#### Module: ![](./.github/images/ledger.png)
The ledger is our 'database', any changes in the way we store information or the data-structures have this tag.

#### Module: ![](./.github/images/house-keeping.png)
'Small' enhancements that need to be done in order to keep the project organized and ensure overall quality. These changes may be applied in any place in code, as long as they are small or do not alter current behavior.

#### Module: ![](./.github/images/network-policy.png)
Identify issues that affect the network-policy like fees, access list or other related issues. Voting may also be related to the network policy module.

#### Module: ![](./.github/images/p2p.png)
This module includes peer-to-peer message exchange and network optmizations, at TCP or UDP level (not HTTP).

#### Module: ![](./.github/images/rpc.png)
All HTTP communication is handled by the RPC module. This module usually provides support methods, since the main communication protocol takes place at the ![](./.github/images/p2p.png) module.

#### Module: ![](./.github/images/vm.png)
New features that affect the Neo Virtual Machine or the Interop layer have this tag.

#### Module: ![](./.github/images/sdk.png)
Neo provides an SDK to help developers to interact with the blockchain. Changes in this module must not impact other parts of the software. 

#### Module: ![](./.github/images/wallet.png)
Wallets are used to track funds and interact with the blockchain. Note that this module depends on a full node implementation (data stored on local disk).

### Commit and PR messages
It is recommended that we use clear commit and PR messages.

Examples:  
- `persistence - rocksdb - Replacing LevelDB with RocksDB`;
- `network - ProtocolHandler - Adding mempool flush methods`;

You can select one module from the list above or use a different word, one that best communicates what has been changed.
A PR should not be denied because of its title.



