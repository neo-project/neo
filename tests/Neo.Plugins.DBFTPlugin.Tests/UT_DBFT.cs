using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using System;
using System.Threading.Tasks;
using Assert = Xunit.Assert;

namespace Neo.Plugins.DBFTPlugin.Tests;

[TestClass]
public partial class UT_DBFT : TestKit
{
    [TestMethod]
    public async Task TestInitializeConsensus()
    {
        // Arrange
        _consensusService.Tell(new ConsensusService.Start());

        var service = _consensusService.UnderlyingActor;
        var context = service.context;

        // Assert
        Assert.True(service.IsStarted);
        Assert.True(context.IsPrimary);
        Assert.NotNull(context.Block);
        Assert.Equal(0, context.ViewNumber);
        Assert.NotNull(context.Validators);
        Assert.Equal(0, context.MyIndex);
        Assert.Null(context.TransactionHashes);
        Assert.Null(context.Transactions);
        Assert.NotNull(context.PreparationPayloads);
        Assert.NotNull(context.CommitPayloads);
        Assert.NotNull(context.ChangeViewPayloads);
        Assert.NotNull(context.LastChangeViewPayloads);
        Assert.NotNull(context.LastSeenMessage);
        Assert.NotNull(context.VerificationContext);
        Assert.NotNull(context.Snapshot);
        Assert.False(context.RequestSentOrReceived);
        Assert.False(context.ResponseSent);
        Assert.False(context.CommitSent);
        Assert.False(context.BlockSent);
        Assert.False(context.ViewChanging);
        Assert.False(context.NotAcceptingPayloadsDueToViewChanging);
        Assert.False(context.MoreThanFNodesCommittedOrLost);
    }

    [TestMethod]
    public async Task TestOnTimer_Primary()
    {
        // Arrange
        SetupConsensusToBePrimary();
        _consensusService.Tell(new ConsensusService.Start());
        await _mockLocalNode.ExpectMsgAsync<LocalNode.SendDirectly>(TimeSpan.FromSeconds(1)); // Ignore initial recovery request

        // Act
        _consensusService.Tell(new ConsensusService.Timer { Height = 1, ViewNumber = 0 });

        // Assert
        var message = await _mockLocalNode.ExpectMsgAsync<LocalNode.SendDirectly>(TimeSpan.FromSeconds(1));
        Assert.NotNull(message);
        Assert.IsType<ExtensiblePayload>(message.Inventory);
        var payload = (ExtensiblePayload)message.Inventory;
        var consensusMessage = ConsensusMessage.DeserializeFrom(payload.Data);
        Assert.IsType<PrepareRequest>(consensusMessage);
    }

    [TestMethod]
    public void TestAddTransaction()
    {
        var transaction = CreateMockTransaction();

        _consensusService.Tell(transaction);

        var message = _mockLocalNode.ExpectMsg<LocalNode.SendDirectly>(TimeSpan.FromSeconds(1));
        Assert.IsType<ExtensiblePayload>(message.Inventory);
        var payload = (ExtensiblePayload)message.Inventory;
        Assert.IsType<PrepareResponse>(ConsensusMessage.DeserializeFrom(payload.Data));
    }

    [TestMethod]
    public async Task TestOnPrepareRequestReceived()
    {
        // Arrange
        _consensusService.Tell(new ConsensusService.Start());
        // await _mockLocalNode.ExpectMsgAsync<LocalNode.SendDirectly>(TimeSpan.FromSeconds(1)); // Ignore initial recovery request
        var prepareRequest = ConsensusContext.MakePrepareRequest();

        // Act
        _consensusService.Tell(new Blockchain.RelayResult
        {
            Inventory = prepareRequest,
            Result = VerifyResult.Succeed
        });
    }

    [TestMethod]
    public void TestOnPrepareResponseReceived()
    {
        SetupConsensusToBePrimary();
        var prepareResponse = CreateMockPrepareResponse();

        _consensusService.Tell(new Blockchain.RelayResult
        {
            Inventory = prepareResponse,
            Result = VerifyResult.Succeed
        });

        // Assuming that receiving enough PrepareResponses triggers a Commit
        var message = _mockLocalNode.ExpectMsg<LocalNode.SendDirectly>(TimeSpan.FromSeconds(1));
        Assert.IsType<ExtensiblePayload>(message.Inventory);
        var payload = (ExtensiblePayload)message.Inventory;
        Assert.IsType<Commit>(ConsensusMessage.DeserializeFrom(payload.Data));
    }

    [TestMethod]
    public async Task TestViewChange()
    {
        _consensusService.Tell(new ConsensusService.Start());
        await Task.Delay(100); // Sleep for 0.5 seconds

        _consensusService.Tell(new Blockchain.RelayResult
        {
            Inventory = ConsensusContext.MakeChangeView(ChangeViewReason.ChangeAgreement),
            Result = VerifyResult.Succeed
        });
    }
}
