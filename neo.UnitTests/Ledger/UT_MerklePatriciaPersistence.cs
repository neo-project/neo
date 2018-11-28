using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MerklePatriciaPersistence
    {
        [TestMethod]
        public void Serialize()
        {
            var mp = new MerklePatricia {["oi"] = "bola"};

            var cloned = new MerklePatricia();
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    mp.Serialize(bw);
                    using (var br = new BinaryReader(bw.BaseStream))
                    {
                        br.BaseStream.Position = 0;
                        cloned.Deserialize(br);
                    }
                }
            }

            Assert.IsTrue(mp.Validate());
            Assert.IsTrue(cloned.Validate());
            Assert.AreEqual("bola", cloned["oi"]);
            Assert.AreEqual(mp, cloned);

            mp["cachorro"] = "cachorro";
            Assert.AreNotEqual(mp, cloned);
        }

        [TestMethod]
        public void SerializeFakingData()
        {
            var mp = new MerklePatricia {["oi"] = "bola"};

            var cloned = new MerklePatricia();
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    mp.Serialize(bw);
                    var data = ms.ToArray();
                    data[data.Length - 2] += 1;
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var binWriter = new BinaryWriter(memoryStream))
                        {
                            binWriter.Write(data);
                            using (var br = new BinaryReader(binWriter.BaseStream))
                            {
                                br.BaseStream.Position = 0;
                                cloned.Deserialize(br);
                            }
                        }
                    }
                }
            }

            Assert.IsTrue(mp.Validate());
            Assert.IsFalse(cloned.Validate());
            Assert.AreNotEqual(mp, cloned);

            Assert.AreEqual("bola", mp["oi"]);
            Assert.AreNotEqual("bola", cloned["oi"]);
        }

        [TestMethod]
        public void SerializeFakingDataBranch()
        {
            var mp = new MerklePatricia {["oi"] = "bola", ["ola"] = "ola"};

            var cloned = new MerklePatricia();
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    mp.Serialize(bw);
                    var data = ms.ToArray();
                    data[data.Length - 2] += 1;
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var binWriter = new BinaryWriter(memoryStream))
                        {
                            binWriter.Write(data);
                            using (var br = new BinaryReader(binWriter.BaseStream))
                            {
                                br.BaseStream.Position = 0;
                                cloned.Deserialize(br);
                            }
                        }
                    }
                }
            }

            Assert.IsTrue(mp.Validate());
            Assert.IsFalse(cloned.Validate());
            Assert.AreNotEqual(mp, cloned);
        }

        [TestMethod]
        public void Clone()
        {
            var mp = new MerklePatricia {["oi"] = "bola"};

            var cloned = mp.Clone();

            Assert.IsTrue(mp.Validate());
            Assert.IsTrue(cloned.Validate());
            Assert.AreEqual("bola", cloned["oi"]);
            Assert.AreEqual(mp, cloned);

            mp["cachorro"] = "cachorro";
            Assert.AreNotEqual(mp, cloned);
        }

        [TestMethod]
        public void FromReplica()
        {
            var mp = new MerklePatricia {["oi"] = "bola"};

            var cloned = new MerklePatricia();
            cloned.FromReplica(mp);

            Assert.IsTrue(mp.Validate());
            Assert.IsTrue(cloned.Validate());
            Assert.AreEqual("bola", cloned["oi"]);
            Assert.AreEqual(mp, cloned);

            mp["cachorro"] = "cachorro";
            Assert.AreNotEqual(mp, cloned);
        }

        [TestMethod]
        public void UsingOnSet()
        {
            var setObj = new HashSet<MerklePatricia>();
            Assert.AreEqual(0, setObj.Count);

            var mpA = new MerklePatricia {["a"] = "a"};
            setObj.Add(mpA);
            Assert.AreEqual(1, setObj.Count);

            var mpB = new MerklePatricia {["b"] = "b"};
            setObj.Add(mpB);
            Assert.AreEqual(2, setObj.Count);

            var mpC = new MerklePatricia {["b"] = "b"};
            setObj.Add(mpC);
            Assert.AreEqual(2, setObj.Count);

            Assert.IsTrue(setObj.Remove(mpA));
            Assert.AreEqual(1, setObj.Count);

            var mpD = new MerklePatricia {["d"] = "d"};
            Assert.IsFalse(setObj.Remove(mpD));
            setObj.Add(mpD);
            Assert.AreEqual(2, setObj.Count);

            setObj.Add(new MerklePatricia());
        }
    }
}