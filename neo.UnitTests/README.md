NEO Unit Tests
====================

This project is a work in progress, aiming to provide unit test coverage for the core NEO code.

Please note that we are aware that we are not using proper isolation / dependency injection / mocking techniques in these tests. To do that would require larger reworks of the base code which is a change for a later date in discussion with the core team, at the moment we are just aiming to get some basic coverage going.

Structure
====================

We use built in Visual Studio functionality with MSTest and the Microsoft.VisualStudio.TestPlatform.TestFramework package. 

To run the tests, build the solution to discover tests, then view and run the tests from the 'Test Explorer' window within Visual Studio.
OR
With .NET Core SDK installed, use the CLI to navigate to the neo.UnitTest folder and use the command 'dotnet restore' to get packages, followed by 'dotnet test' to run tests.

Coverage
====================

* Base
	* Fixed8.cs
* Core
	* AccountState.cs
	* AssetState.cs
	* Block.cs - Some code coverage missing on the Verify() method.
	* ClaimTransaction.cs	
	* CoinReference.cs	
	* Header.cs
	* Helper.cs
	* InvocationTransaction.cs
	* IssueTransaction.cs
	* MinerTransaction.cs
	* SpentCoin.cs
	* SpentCoinState.cs
	* StorageItem.cs
	* StorageKey.cs
	* TransactionAttribute.cs
	* TransactionOuput.cs
	* TransactionResult.cs
	* UnspentCoinState.cs
	* ValidatorState.cs
	* VoteState.cs
	* Witness.cs