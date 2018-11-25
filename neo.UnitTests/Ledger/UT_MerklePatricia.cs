using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MerklePatricia
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
        public void DistinctRoot()
        {
            var mp = new MerklePatricia();
            Assert.IsFalse(mp == null);
            mp[new byte[] {0x0, 0x0, 0x1}] = new byte[] {0x0, 0x0, 0x1};
            Assert.IsTrue(new byte[] {0x0, 0x0, 0x1}.SequenceEqual(mp[new byte[] {0x0, 0x0, 0x1}]));

            mp[new byte[] {0x11, 0x0, 0x2}] = new byte[] {0x11, 0x0, 0x2};
            Assert.IsTrue(new byte[] {0x11, 0x0, 0x2}.SequenceEqual(mp[new byte[] {0x11, 0x0, 0x2}]));
        }

        [TestMethod]
        public void SameRoot()
        {
            var mp = new MerklePatricia();
            Assert.IsFalse(mp == null);
            mp[new byte[] {0x0, 0x0, 0x1}] = new byte[] {0x0, 0x0, 0x1};
            Assert.IsTrue(new byte[] {0x0, 0x0, 0x1}.SequenceEqual(mp[new byte[] {0x0, 0x0, 0x1}]));

            mp[new byte[] {0x0, 0x0, 0x1}] = new byte[] {0x0, 0x0, 0x2};
            Assert.IsTrue(new byte[] {0x0, 0x0, 0x2}.SequenceEqual(mp[new byte[] {0x0, 0x0, 0x1}]));
        }

        [TestMethod]
        public void ColideKeys()
        {
            var mp = new MerklePatricia
            {
                ["oi"] = "batata",
                ["oi"] = "batatatinha"
            };
            Assert.IsTrue(mp.ContainsKey("oi"));
            Assert.AreEqual("batatatinha", mp["oi"]);
            Assert.IsTrue(mp.Validate());

            mp["orelha"] = "batatatinha";
            Assert.AreEqual("batatatinha", mp["orelha"]);
            Assert.IsTrue(mp.Validate());

            mp["orfão"] = "criança";
            Assert.AreEqual("criança", mp["orfão"]);
            Assert.IsTrue(mp.Validate());

            mp["orfanato"] = "crianças";
            Assert.AreEqual("crianças", mp["orfanato"]);
            Assert.IsTrue(mp.Validate());

            Assert.IsTrue(mp.Remove("orfanato"));
            Assert.AreEqual("criança", mp["orfão"]);
            Assert.IsFalse(mp.ContainsKey("orfanato"));
            Assert.IsTrue(mp.Validate());

            mp["orfã"] = "menina";
            Assert.AreEqual("menina", mp["orfã"]);
            Assert.IsTrue(mp.Validate());
        }

        [TestMethod]
        public void StepByStep()
        {
            var mp = new MerklePatricia();
            Assert.AreEqual(0, mp.Count());
            var a0001 = new byte[] {0x0, 0x01};
            mp[a0001] = a0001;
            Assert.AreEqual(1, mp.Count());
            Assert.AreEqual(a0001, mp[a0001]);

            var a0101 = new byte[] {0x01, 0x01};
            mp[a0101] = a0101;
            Assert.AreEqual(2, mp.Count());
            Assert.AreEqual(a0001, mp[a0001]);
            Assert.AreEqual(a0101, mp[a0101]);

            Assert.IsTrue(mp.Remove(a0101));
            Assert.IsFalse(mp.ContainsKey(a0101));
            Assert.AreEqual(a0001, mp[a0001]);

            Assert.AreEqual(new MerklePatricia {[a0001] = a0001}, mp);
        }

        [TestMethod]
        public void ContainsValue()
        {
            var mp = new MerklePatricia
            {
                ["aoi"] = "oi",
                ["boi2"] = "oi2",
                ["coi1"] = "oi3"
            };
            Assert.IsTrue(mp.Validate());
            Assert.IsTrue(mp.ContainsValue("oi"));
            Assert.IsTrue(mp.ContainsValue("oi2"));
            Assert.IsTrue(mp.ContainsValue("oi3"));

            Assert.IsFalse(mp.ContainsValue("aoi"));
            Assert.IsFalse(mp.ContainsValue("boi2"));
            Assert.IsFalse(mp.ContainsValue("coi3"));
            Assert.IsTrue(mp.Validate());
        }

        [TestMethod]
        public void PatriciaCount()
        {
            var mp = new MerklePatricia();
            Assert.AreEqual(0, mp.Count());
            Assert.IsTrue(mp.Validate());

            mp["oi"] = "oi";
            Assert.AreEqual(1, mp.Count());
            Assert.IsTrue(mp.Validate());

            mp["oi"] = "oi";
            Assert.AreEqual(1, mp.Count());
            Assert.IsTrue(mp.Validate());

            mp["oi"] = "oi1";
            Assert.AreEqual(1, mp.Count());

            mp["oi1"] = "oi1";
            mp["oi2"] = "oi2";
            Assert.AreEqual(3, mp.Count());

            mp["bala"] = "bala2";
            Assert.AreEqual(4, mp.Count());
            Assert.IsTrue(mp.Validate());
        }

        [TestMethod]
        public void Dictionary()
        {
            var exemplo = new Dictionary<string, string>
            {
                ["oi"] = "bala",
                ["oi1"] = "bala1",
                ["oi2"] = "bala2",
                ["oi12"] = "bala12",
                ["bola"] = "oi",
                ["birosca123"] = "bruca123",
                ["ca123"] = "que123",
                ["oi123"] = "bala123"
            };

            var merklePatricia = new MerklePatricia();
            foreach (var keyValue in exemplo)
            {
                merklePatricia[keyValue.Key] = keyValue.Value;
            }

            Assert.IsTrue(merklePatricia.Validate());

            foreach (var keyValue in exemplo)
            {
                Assert.AreEqual(keyValue.Value, merklePatricia[keyValue.Key]);
            }

            Assert.IsTrue(merklePatricia.Validate());
        }

        [TestMethod]
        public void Lista()
        {
            var lista = new[] {"oi", "oi1", "oi2", "oi12", "bola", "birosca123", "ca123", "oi123"};
            var mp = new MerklePatricia();
            foreach (var it in lista)
            {
                mp[it] = it;
                System.Console.WriteLine(
                    $"Linha: '{it}:{Encoding.UTF8.GetBytes(it).ByteToHexString(false, false)}':\n{mp}");
                Assert.AreEqual(it, mp[it]);
                Assert.IsTrue(mp.Validate());
            }
        }

        [TestMethod]
        public void One()
        {
            var merklePatricia = new MerklePatricia();
            Assert.IsTrue(merklePatricia.Validate());
//            Assert.AreEqual(0, merklePatricia.Height());

            void InserirTestar(string x, string y)
            {
                merklePatricia[x] = y;
                Assert.IsTrue(merklePatricia.Validate());
                Assert.IsTrue(merklePatricia.ContainsKey(x));
                Assert.IsFalse(merklePatricia.ContainsKey(x + "123"));
                Assert.AreEqual(y, merklePatricia[x]);
                Assert.IsNull(merklePatricia[x + "123k"]);
            }

            InserirTestar("01a2", "valor1");
//            Assert.AreEqual(1, merklePatricia.Height());
            Assert.IsTrue(merklePatricia.Validate());

            InserirTestar("11a2", "valor2");
            Assert.IsTrue(merklePatricia.Validate());

            InserirTestar("0212", "valor3");
//            Assert.Equal(3, merklePatricia.Height());
            Assert.IsTrue(merklePatricia.Validate());

            merklePatricia["0"] = "valor4";
            Assert.IsTrue(merklePatricia.Validate());
        }

        [TestMethod]
        public void Remove()
        {
            var mp = new MerklePatricia();
            Assert.IsTrue(mp.Validate());

            void RemoverTestar(string x, string y)
            {
                mp[x] = y;
                Assert.IsTrue(mp.ContainsKey(x));
                Assert.IsFalse(mp.ContainsKey(x + "123"));
                Assert.AreEqual(y, mp[x]);
                Assert.IsNull(mp[x + "123k"]);

                Assert.IsTrue(mp.Remove(x));
                Assert.IsFalse(mp.Remove(x));
            }

            RemoverTestar("oi", "bala");
            Assert.IsTrue(mp.Validate());
            mp.Remove("oi");
            Assert.IsFalse(mp.ContainsKey("oi"));
            Assert.IsTrue(mp.Validate());

            mp["123"] = "abc";
            mp["a123"] = "1abc";
            Assert.AreEqual(2, mp.Count());
            Assert.IsTrue(mp.Validate());

            Assert.IsFalse(mp.Remove("b123"));
            Assert.AreEqual(2, mp.Count());
            Assert.IsTrue(mp.Remove("a123"));
            Assert.IsTrue(mp.Validate());
            Assert.AreEqual(1, mp.Count());
            Assert.IsFalse(mp.ContainsKey("a123"));
            Assert.IsTrue(mp.ContainsKey("123"));
            Assert.IsTrue(mp.ContainsKey("123"));
            Assert.IsTrue(mp.Validate());

            var mp2 = new MerklePatricia {["123"] = "abc"};
            Assert.AreEqual(mp2, mp);
            Assert.IsTrue(mp.Validate());
            Assert.IsTrue(mp2.Validate());
        }

        [TestMethod]
        public void EqualsThree()
        {
            var mpA = new MerklePatricia
            {
                ["oi"] = "bola",
                ["oi1"] = "1bola",
                ["oi2"] = "b2ola",
                ["oi1"] = "bola1"
            };
            Assert.IsTrue(mpA.Validate());

            var mpB = new MerklePatricia
            {
                ["oi"] = "bola",
                ["oi1"] = "1bola",
                ["oi2"] = "b2ola",
                ["oi1"] = "bola1"
            };
            Assert.IsTrue(mpB.Validate());
            Assert.AreEqual(mpA, mpB);

            mpA["oi"] = "escola";
            Assert.AreNotEqual(mpA, mpB);
            Assert.IsTrue(mpA.Validate());

            mpB["oi"] = "escola";
            Assert.AreEqual(mpA, mpB);
            Assert.IsTrue(mpB.Validate());

            mpA["oi123"] = "escola";
            mpA["oi12"] = "escola1";
            mpA["bola"] = "escola2";
            mpA["dog"] = "escola2";
            Assert.IsTrue(mpA.Validate());

            mpB["bola"] = "escola2";
            mpB["dog"] = "escola2";
            mpB["oi12"] = "escola1";
            mpB["oi123"] = "escola";
            Assert.AreEqual(mpA, mpB);
            Assert.IsTrue(mpB.Validate());

            mpA.Remove("oi");
            mpA.Remove("oi");
            Assert.AreNotEqual(mpA, mpB);
            Assert.IsTrue(mpA.Validate());

            mpB.Remove("oi");
            Assert.AreEqual(mpA, mpB);
            Assert.IsTrue(mpB.Validate());

            Assert.IsNull(mpA["Meg"]);

            Assert.ThrowsException<ArgumentNullException>(() => mpA[(string) null]);
            Assert.ThrowsException<ArgumentNullException>(() => mpA[(string) null] = null);
            Assert.ThrowsException<ArgumentNullException>(() => mpA[(byte[]) null]);
            Assert.ThrowsException<ArgumentNullException>(() => mpA[(byte[]) null] = null);
            Assert.ThrowsException<ArgumentNullException>(() => mpA[new byte[] {0, 1}] = null);
            Assert.ThrowsException<ArgumentNullException>(() => mpA["Meg"] = null);
        }
    }
}