
# Contributing to NEO
Neo is an open-source project and it depends on its contributors and constant community feedback to implement the features required for an smart economy.  

You are more than welcome to join us in the development of Neo.  
Please read this whole document in order to understand how issues are organized and how you can start contributing.

<div style="background:#fafafa; border-radius:3px; padding:8px 10px; font-style:italic">This document covers <a href="">neo 
</a> repository only and does not include community repositories or repositories managed by NGD Shanghai and NGD Seattle.</div>

### Questions and Support
The issue list is reserved exclusively for bug reports and features discussions. If you have questions or need support, please visit us in our [Discord](https://discord.io/neo) server.  

### dApp Development
This document does not relate to dApp development. If you are looking to build a dApp using Neo, please [start here](https://neo.org/dev).

### Developer Guidance
We try to keep the minimum rules possible, just enough to keep the project organized. If you want to suggest changes in this document, please create an issue or message us in Discord.

##### Use complete titles
Use a descriptive titles and descriptions in your PRs. They may be used in our monthly report.  

##### Discuss before coding
Proposals must be discussed before being implemented. Create a new issue if you have an idea of a new feature or enhancement. Avoid implementing issues with the <span style="background:#5319e7; border-radius:3px; padding:2px 4px; color:white">discussion</span> tag.

##### Tests during code review
We expect reviewers to test the issue before approving or requesting changes.

##### Give time to other developers review an issue
Even if the code has been approved, you should leave at least 24 hours for others to review it before merging the code.

##### Read the PR template
Make sure you follow the instructions in our PR template to ensure your code is not rejected.

##### Create unit tests
Since all issues must be tested by the reviewers, it is important that the developer includes basic unit tests.

##### Task assignment
If developers want to work in a specific issue, he may ask the team to assign it to himself. The proposer of an issue has priority in task assignment.


### Issues for begginners
If you are looking to start contributing to NEO, we suggest you start working on issues with <span style="background:#782360; border-radius:3px; padding:2px 4px; color:white">cosmetic</span> or <span style="background:#c44672; border-radius:3px; padding:2px 4px; color:white">house-keeping</span> tags, since they usually do not depend on extensive NEO platform knowledge. 

### Issues State
Usually, issues follow the the <span style="background:#5319e7; border-radius:3px; padding:2px 4px; color:white">discussion</span> - <span style="background:#cc317c; border-radius:3px; padding:2px 4px; color:white">solution-design</span> - <span style="background:#557325; border-radius:3px; padding:2px 4px; color:white">ready-to-implement</span> - <span style="background:#1e536e; border-radius:3px; padding:2px 4px; color:white">to-review</span> flow, but this is not strictly mandatory.

#### Initial state: <span style="background:#5319e7; border-radius:3px; padding:2px 4px; color:white">discussion</span>
Whenever someone posts a new feature request, it is tagged with <span style="background:#5319e7; border-radius:3px; padding:2px 4px; color:white">state: discussion</span>. This means that there is no consensus if the feature should be implemented or not. Avoid creating PR to solve issues in this state since it may be completely discarded.

#### Issue state: <span style="background:#cc317c; border-radius:3px; padding:2px 4px; color:white">solution-design</span>
When a feature request is accepted by the team, but there is no consensus in the solution design, the issue is tagged with <span style="background:#cc317c; border-radius:3px; padding:2px 4px; color:white">solution-design</span>. We recommend the team to agree in the solution design before anyone attempts to implement it, using text or UML. It is not recommended, but developers can also present his solution using code.  
Note that PRs for issues in this state may also be discarded if the team disagree with the proposed solution.

#### Issue state: <span style="background:#557325; border-radius:3px; padding:2px 4px; color:white">ready-to-implement</span>
Once the the team has agreed on feature and the proposed solution, the issue is tagged with <span style="background:#557325; border-radius:3px; padding:2px 4px; color:white">ready-to-implement</span>. When implementing it, please follow the solution accepted by the team.

#### Issue state: <span style="background:#1e536e; border-radius:3px; padding:2px 4px; color:white">to-review</span>
Some issues need to be reviewed by the team. May be used in issues that need attention or to create task lists.

### Issue Types


#### Type: <span style="background:#782360; border-radius:3px; padding:2px 4px; color:white">cosmetic</span>

Issues with the <span style="background:#782360; border-radius:3px; padding:2px 4px; color:white">cosmetic</span> tag are usually changes in code or documentation that improves user experience without affecting current functionality. These issues are recommended for begginners because they require little to no knowledge about Neo platform.

#### Type: <span style="background:#1b4866; border-radius:3px; padding:2px 4px; color:white">enhancement</span>

Enhancements are platform changes that may affect performance, usability or add new features to existing modules. Is recommended that developers have previous knowledge in the platform to work in these improvements, specially in more complicated modules like the <span style="background:#0d1891; border-radius:3px; padding:2px 4px; color:white">compiler</span>, <span style="background:#1a547a; border-radius:3px; padding:2px 4px; color:white">ledger</span> and <span style="background:#590daa; border-radius:3px; padding:2px 4px; color:white">consensus</span> modules.

#### Type: <span style="background:#117d89; border-radius:3px; padding:2px 4px; color:white">new-feature</span>

New features usually include large changes in the code base, but not always these large changes are of high complexity, so some issues with <span style="background:#117d89; border-radius:3px; padding:2px 4px; color:white">new-feature</span> issues may be recommended for starters, specially those related to the <span style="background:#423316; border-radius:3px; padding:2px 4px; color:white">rpc</span> and the <span style="background:#68756e; border-radius:3px; padding:2px 4px; color:white">sdk</span> module.

#### Type: <span style="background:#331b66; border-radius:3px; padding:2px 4px; color:white">migration</span>
Coding relaed to the migration (Neo 2 to Neo 3) are tagged with <span style="background:#331b66; border-radius:3px; padding:2px 4px; color:white">migration</span>. These issues are usually the most complicated ones, since they required a deep knowledge in both Neo 2 and Neo 3.

### Tags for Project Modules 
These tags do not necesseraly represent each module at code levels. Modules <span style="background:#590daa; border-radius:3px; padding:2px 4px; color:white">consensus</span> and <span style="background:#0d1891; border-radius:3px; padding:2px 4px; color:white">compiler</span> are not recommended for begginers.

#### Module: <span style="background:#0d1891; border-radius:3px; padding:2px 4px; color:white">compiler</span>
Issues that are related or influence the behavior of our C# compiler. Note that the compiler itself is hosted in the [neo-devpack-dotnet](https://github.com/neo-project/neo-devpack-dotnet) repository.

#### Module: <span style="background:#590daa; border-radius:3px; padding:2px 4px; color:white">consensus</span>
Changes to consensus are usually harder to make and test. Avoid implementing issues in this module that are not yet decided.

#### Module: <span style="background:#1a547a; border-radius:3px; padding:2px 4px; color:white">ledger</span>
The ledger is our 'database', any changes in the way we store information or the data-structures have this tag.

#### Module: <span style="background:#c44672; border-radius:3px; padding:2px 4px; color:white">house-keeping</span>
'Small' enhancements that need to be done in order to keep the project organized and overall quality. These changes may be applied in any place in code, as long they are small or do not alter current behavior.

#### Module: <span style="background:#204221; border-radius:3px; padding:2px 4px; color:white">network-policy</span>
Identify issues that affect the network-policy, like fees, access list, or other network policy related issues. Voting may also be related to the network policy module.

#### Module: <span style="background:#201c33; border-radius:3px; padding:2px 4px; color:white">p2p</span>
This module includes peer-to-peer message exchange and network optmizations, at TCP or UDP level (not HTTP).

#### Module: <span style="background:#423316; border-radius:3px; padding:2px 4px; color:white">rpc</span>
All HTTP communication is handled by the RPC module. This module usually provides support methods, since the main communication protocol takes place at the <span style="background:#201c33; border-radius:3px; padding:2px 4px; color:white">p2p</span> module.

#### Module: <span style="background:#620069; border-radius:3px; padding:2px 4px; color:white">vm</span>
New features that affect the Neo Virtual Machine or the Interop layer have this tag.

#### Module: <span style="background:#68756e; border-radius:3px; padding:2px 4px; color:white">sdk</span>
Neo provides an SDK to help developers to interact with the blockchain. Changes in this module must not impact other parts of the software. 

#### Module: <span style="background:#0b5e24; border-radius:3px; padding:2px 4px; color:white">wallet</span>
Wallets are used to track funds and interact with the blockchain. Note that this module uses depends on a full node implementation (data stored on local disk).

### Commit and PR messages
It is recommended that we use clear commit and PR messages.

Examples:  
- `persistence - rocksdb - Replacing LevelDB with RocksDB`;
- `network - ProtocolHandler - Adding mempool flush methods`;

You can select one module from the list use a different word, one that best clarifies what have been changed.

The message format is a suggestions. PRs should not be denied by it's message.



