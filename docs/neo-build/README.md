# Neo Build
This engine, which is also known as `NEBuild`, provides a `JSON` schema
for a project file that controls how the build platform processes and
builds contracts. `Visual Studio` uses `NEBuild`, but `NEBuild` doesn't
depend on `Visual Studio`. By invoking `nebuild.exe` or `nebuild` on
your project, you can orchestrate and build contracts in environments
where `Visual Studio` isn't installed.

`Visual Studio` uses `NEBuild` to load and build Neo projects. The project
files in `Visual Studio` (`*.csproj` and others) contain `MSBuild` `XML`
code that executes when you build a project in the `IDE`. `Visual Studio`
projects import all the necessary settings (`*.nbproj` files) and build
processes to execute `NEBuild` tasks, but you can extend or modify them
from within `Visual Studio` or by using a text editor.

## What is Neo Build
`NEBuild`, or the Neo Build Engine, is a platform that automates
the process of building, deploying and testing Neo blockchain contracts.
It uses a `JSON` schema to control how the build, deploy and test Neo
platform processes and builds or deploys contracts, transactions and/or
testing scenarios.

## What NEBuild does
1. Compile contracts
2. Package contracts
3. Test contracts
4. Deploy contracts and nodes
5. Create wallets and transactions

## How NEBuild is used with
- `dotnet`
- `Visual Studio`
- `Visual Studio Code`
- `MSBuild`

## NEBuild can be used to
1. **Deploy**
   - Contracts
   - Nodes
   - Tests
   - Transactions
   - Projects
2. **Create**
   - Contracts
   - Transactions
   - Wallets
   - Testing scenarios
   - Projects

## How NEBuild works
- NEBuild uses a JSON schema to instruct `dotnet` or `MSBuild` on how to compile the project.
- NEBuild uses tasks within `Visual Studio`, which are independent executable components with inputs and outputs.
- NEBuild uses targets within `Visual Studio`, which are named sequences of tasks that represent something to be built or done.
