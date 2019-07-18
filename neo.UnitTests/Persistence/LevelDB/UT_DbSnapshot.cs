using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.LevelDB;
using System;
using System.IO;
using System.Threading;

namespace Neo.UnitTests.Persistence.LevelDB
{
    [TestClass]
    public class UT_DbSnapshot
    {
        private Snapshot dbSnapshot;

        private LevelDBStore store;

        private string dbPath;

        [TestInitialize]
        public void TestSetup()
        {
            string threadName = Thread.CurrentThread.ManagedThreadId.ToString();
            dbPath = Path.GetFullPath(nameof(UT_DbSnapshot) + string.Format("_Chain_{0}", new Random().Next(1, 1000000).ToString("X8")) + threadName);
            if (store == null)
            {
                store = new LevelDBStore(dbPath);
            }
        }

        [TestCleanup]
        public void DeleteDir()
        {
            store.Dispose();
            store = null;
            TestUtils.DeleteFile(dbPath);
        }

        [TestMethod]
        public void TestCommitAndDispose()
        {
            dbSnapshot = store.GetSnapshot();

            Transaction tx = new Transaction();
            tx.Script = TestUtils.GetByteArray(32, 0x42);
            tx.Sender = UInt160.Zero;
            tx.SystemFee = 4200000000;
            tx.Attributes = new TransactionAttribute[0];
            tx.Witnesses = new[]
            {
                new Witness
                {
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                }
            };

            TransactionState txState = new TransactionState();
            txState.Transaction = tx;
            txState.BlockIndex = 10;
            dbSnapshot.Transactions.Add(tx.Hash, txState);
            dbSnapshot.Commit();
            Snapshot newSanpshot = store.GetSnapshot();
            Transaction internalTx = newSanpshot.GetTransaction(tx.Hash);
            newSanpshot.Dispose();
            Assert.AreEqual(tx, internalTx);
        }
    }
}