using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.VM;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_InteropPrices
    {
        NeoService uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new NeoService(TriggerType.Application, null);
        }

        [TestMethod]
        public void NeoServiceFixedPrices()
        {
            uut.GetPrice("Neo.Runtime.GetTrigger".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Runtime.CheckWitness".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("Neo.Runtime.Notify".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Runtime.Log".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Runtime.GetTime".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Runtime.Serialize".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Runtime.Deserialize".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Blockchain.GetHeight".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Blockchain.GetHeader".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("Neo.Blockchain.GetBlock".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("Neo.Blockchain.GetTransaction".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("Neo.Blockchain.GetTransactionHeight".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("Neo.Blockchain.GetAccount".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("Neo.Blockchain.GetValidators".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("Neo.Blockchain.GetAsset".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("Neo.Blockchain.GetContract".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("Neo.Header.GetHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Header.GetVersion".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Header.GetPrevHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Header.GetMerkleRoot".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Header.GetTimestamp".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Header.GetIndex".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Header.GetConsensusData".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Header.GetNextConsensus".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Block.GetTransactionCount".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Block.GetTransactions".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Block.GetTransaction".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Transaction.GetHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Transaction.GetType".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Transaction.GetAttributes".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Transaction.GetInputs".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Transaction.GetOutputs".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Transaction.GetReferences".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("Neo.Transaction.GetUnspentCoins".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("Neo.Transaction.GetWitnesses".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("Neo.InvocationTransaction.GetScript".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Witness.GetVerificationScript".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("Neo.Attribute.GetUsage".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Attribute.GetData".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Input.GetHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Input.GetIndex".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Output.GetAssetId".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Output.GetValue".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Output.GetScriptHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Account.GetScriptHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Account.GetVotes".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Account.GetBalance".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Account.IsStandard".ToInteropMethodHash()).Should().Be(100);
            //uut.GetPrice("Neo.Asset.Create".ToInteropMethodHash()).Should().Be(100); Asset_Create);
            //uut.GetPrice("Neo.Asset.Renew".ToInteropMethodHash()).Should().Be(100); Asset_Renew);
            uut.GetPrice("Neo.Asset.GetAssetId".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Asset.GetAssetType".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Asset.GetAmount".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Asset.GetAvailable".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Asset.GetPrecision".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Asset.GetOwner".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Asset.GetAdmin".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Asset.GetIssuer".ToInteropMethodHash()).Should().Be(1);
            //uut.GetPrice("Neo.Contract.Create".ToInteropMethodHash()).Should().Be(100); Contract_Create);
            //uut.GetPrice("Neo.Contract.Migrate".ToInteropMethodHash()).Should().Be(100); Contract_Migrate);
            uut.GetPrice("Neo.Contract.Destroy".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Contract.GetScript".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Contract.IsPayable".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Contract.GetStorageContext".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Storage.GetContext".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Storage.GetReadOnlyContext".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Storage.Get".ToInteropMethodHash()).Should().Be(100);
            //uut.GetPrice("Neo.Storage.Put".ToInteropMethodHash()).Should().Be(100); Storage_Put);
            uut.GetPrice("Neo.Storage.Delete".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("Neo.Storage.Find".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.StorageContext.AsReadOnly".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Enumerator.Create".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Enumerator.Next".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Enumerator.Value".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Enumerator.Concat".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Iterator.Create".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Iterator.Key".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Iterator.Keys".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Iterator.Values".ToInteropMethodHash()).Should().Be(1);

            #region Aliases
            uut.GetPrice("Neo.Iterator.Next".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Iterator.Value".ToInteropMethodHash()).Should().Be(1);
            #endregion

            #region Old APIs
            uut.GetPrice("AntShares.Runtime.CheckWitness".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("AntShares.Runtime.Notify".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Runtime.Log".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Blockchain.GetHeight".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Blockchain.GetHeader".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("AntShares.Blockchain.GetBlock".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("AntShares.Blockchain.GetTransaction".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("AntShares.Blockchain.GetAccount".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("AntShares.Blockchain.GetValidators".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("AntShares.Blockchain.GetAsset".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("AntShares.Blockchain.GetContract".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("AntShares.Header.GetHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Header.GetVersion".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Header.GetPrevHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Header.GetMerkleRoot".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Header.GetTimestamp".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Header.GetConsensusData".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Header.GetNextConsensus".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Block.GetTransactionCount".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Block.GetTransactions".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Block.GetTransaction".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Transaction.GetHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Transaction.GetType".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Transaction.GetAttributes".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Transaction.GetInputs".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Transaction.GetOutputs".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Transaction.GetReferences".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("AntShares.Attribute.GetUsage".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Attribute.GetData".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Input.GetHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Input.GetIndex".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Output.GetAssetId".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Output.GetValue".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Output.GetScriptHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Account.GetScriptHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Account.GetVotes".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Account.GetBalance".ToInteropMethodHash()).Should().Be(1);
            //uut.GetPrice("AntShares.Asset.Create".ToInteropMethodHash()).Should().Be(100); Asset_Create);
            //uut.GetPrice("AntShares.Asset.Renew".ToInteropMethodHash()).Should().Be(100); Asset_Renew);
            uut.GetPrice("AntShares.Asset.GetAssetId".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Asset.GetAssetType".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Asset.GetAmount".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Asset.GetAvailable".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Asset.GetPrecision".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Asset.GetOwner".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Asset.GetAdmin".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Asset.GetIssuer".ToInteropMethodHash()).Should().Be(1);
            //uut.GetPrice("AntShares.Contract.Create".ToInteropMethodHash()).Should().Be(100); Contract_Create);
            //uut.GetPrice("AntShares.Contract.Migrate".ToInteropMethodHash()).Should().Be(100); Contract_Migrate);
            uut.GetPrice("AntShares.Contract.Destroy".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Contract.GetScript".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Contract.GetStorageContext".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Storage.GetContext".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("AntShares.Storage.Get".ToInteropMethodHash()).Should().Be(100);
            //uut.GetPrice("AntShares.Storage.Put".ToInteropMethodHash()).Should().Be(100); Storage_Put);
            uut.GetPrice("AntShares.Storage.Delete".ToInteropMethodHash()).Should().Be(100);
            #endregion
        }
    }
}
