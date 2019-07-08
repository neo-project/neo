using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence.LevelDB;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Neo.Network.P2P.Payloads;
using Neo.Ledger;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_DbSnapshot
    {
        private Snapshot dbSnapshot;

        private LevelDBStore store;

        [TestInitialize]
        public void TestSetup()
        {
            if (store == null)
            {
                store = new LevelDBStore(Path.GetFullPath(string.Format("Chain_{0}", 123456.ToString("X8"))));
            }
            dbSnapshot = store.GetSnapshot();
        }

        [TestCleanup]
        public void TestEnd() {
            store.Dispose();
        }

        [TestMethod]
        public void TestCommitAndDispose() {

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
            dbSnapshot.Transactions.Add(tx.Hash,txState);
            dbSnapshot.Commit();
            Snapshot newSanpshot = store.GetSnapshot();
            Transaction internalTx = newSanpshot.GetTransaction(tx.Hash);
            newSanpshot.Dispose();
            Assert.AreEqual(tx,internalTx);

        }

        
        [ClassCleanup]
        public static void DeleteDir()
        {
            string file = Path.GetFullPath(string.Format("Chain_{0}", 123456.ToString("X8")));
            DeleteFile(file);
        }

        private static void DeleteFile(string file) {
            System.IO.DirectoryInfo fileInfo = new DirectoryInfo(file);
            fileInfo.Attributes = FileAttributes.Normal & FileAttributes.Directory;
            System.IO.File.SetAttributes(file, System.IO.FileAttributes.Normal);
            if (Directory.Exists(file))
            {
                foreach (string f in Directory.GetFileSystemEntries(file))
                {
                    if (File.Exists(f))
                    {
                        File.Delete(f);
                    }
                    else
                    {
                        DeleteFile(f);
                    }
                }
                Directory.Delete(file);
            }
        }
    }
}
