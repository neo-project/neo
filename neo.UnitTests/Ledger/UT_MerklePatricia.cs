using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Ledger.MPT;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MerklePatricia
    {
        private static void EqualsAfterRemove(List<string> values, int selected = 0, int? max = null)
        {
            values = new List<string>(new HashSet<string>(values));
            for (max = max ?? 1 << values.Count; selected < max; selected++)
            {
                var inserted = new MerklePatricia();
                var insertedRemoved = new MerklePatricia();
                var insertedRemovedBackwards = new MerklePatricia();

                for (var i = 0; i < values.Count; i++)
                {
                    if (selected.GetBit(i))
                    {
                        inserted[values[i]] = values[i];
                    }

                    insertedRemoved[values[i]] = values[i];
                    insertedRemovedBackwards[values[i]] = values[i];
                }

                for (var i = 0; i < values.Count; i++)
                {
                    if (selected.GetBit(i)) continue;
                    insertedRemoved.Remove(values[i]);
                }

                for (var i = values.Count - 1; i >= 0; i--)
                {
                    if (selected.GetBit(i)) continue;
                    insertedRemovedBackwards.Remove(values[i]);
                }

                var testMessage = $"Test {selected}/{max}";
                var testBackwardsMessage = $"Test backwards {selected}/{max}";
                for (var i = 0; i < values.Count; i++)
                {
                    if (selected.GetBit(i))
                    {
                        Assert.AreEqual(values[i], inserted[values[i]], testMessage);
                        Assert.AreEqual(values[i], insertedRemoved[values[i]], testMessage);
                        Assert.AreEqual(values[i], insertedRemovedBackwards[values[i]], testBackwardsMessage);
                    }
                    else
                    {
                        Assert.IsFalse(inserted.ContainsKey(values[i]), testMessage);
                        Assert.IsFalse(insertedRemoved.ContainsKey(values[i]), testMessage);
                        Assert.IsFalse(insertedRemovedBackwards.ContainsKey(values[i]), testBackwardsMessage);
                    }
                }

                Assert.AreEqual(inserted, insertedRemoved, testMessage);
                Assert.AreEqual(inserted, insertedRemovedBackwards, testBackwardsMessage);
                // At this point insertedRemoved should be equals to insertedRemovedBackwards so it must be
                Assert.AreEqual(insertedRemoved, insertedRemovedBackwards, testBackwardsMessage);
            }
        }

        [TestMethod]
        public void EqualsAfterRemove()
        {
            EqualsAfterRemove(new List<string> {"a", "ab", "ba", "bb", "zba"}, 3);

            EqualsAfterRemove(new List<string> {"a"});
            EqualsAfterRemove(new List<string> {"a", "ab"});
            EqualsAfterRemove(new List<string> {"a", "ab", "ba"});
            EqualsAfterRemove(new List<string> {"a", "ab", "ba", "bb"});
            EqualsAfterRemove(new List<string> {"a", "ab", "ba", "bb", "zba"});
            EqualsAfterRemove(new List<string> {"a", "ab", "ba", "bb", "zba", "cba"});
            EqualsAfterRemove(new List<string> {"a", "b", "aaaa", "baaa", "aaab", "baab"});

            EqualsAfterRemove(new List<string> {"a", "b", "aaaa", "baaa", "aaab", "baab"});
        }

        private static void CheckCopy(MerklePatricia mp)
        {
            var cloned = new MerklePatricia();
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                mp.Serialize(bw);
                using (var br = new BinaryReader(bw.BaseStream))
                {
                    br.BaseStream.Position = 0;
                    cloned.Deserialize(br);
                }
            }

            Assert.IsTrue(mp.Validate());
            Assert.IsTrue(cloned.Validate());
            Assert.AreEqual(mp, cloned);

            cloned = mp.Clone();
            Assert.IsTrue(mp.Validate());
            Assert.IsTrue(cloned.Validate());
            Assert.AreEqual(mp, cloned);

            cloned = new MerklePatricia();
            cloned.FromReplica(mp);
            Assert.IsTrue(mp.Validate());
            Assert.IsTrue(cloned.Validate());
            Assert.AreEqual(mp, cloned);
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
            Assert.IsNull(mp[new byte[] {0x20, 0x0, 0x2}]);

            CheckCopy(mp);
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

            CheckCopy(mp);
        }

        [TestMethod]
        public void EndsOnExtension()
        {
            var mp = new MerklePatricia {["bola"] = "bola", ["bolo"] = "bolo"};
            Assert.AreEqual("bola", mp["bola"]);
            Assert.AreEqual("bolo", mp["bolo"]);
            Assert.AreEqual(2, mp.Count());
            mp["bol"] = "bol";
            Assert.AreEqual("bol", mp["bol"]);
            mp["aol"] = "aol";
            Assert.AreEqual("aol", mp["aol"]);
            mp["zol"] = "zol";
            Assert.AreEqual("zol", mp["zol"]);
            mp["bala"] = "bala";
            Assert.AreEqual("bala", mp["bala"]);
            mp["bo"] = "bo";
            Assert.AreEqual("bo", mp["bo"]);
            Assert.AreEqual(7, mp.Count());

            Assert.AreEqual("aol", mp["aol"]);
            Assert.AreEqual("zol", mp["zol"]);
            Assert.AreEqual("bol", mp["bol"]);
            Assert.AreEqual("bola", mp["bola"]);
            Assert.AreEqual("bolo", mp["bolo"]);

            CheckCopy(mp);
        }

        [TestMethod]
        public void ColidKeys()
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

            CheckCopy(mp);
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

            CheckCopy(mp);
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

            CheckCopy(mp);
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

            CheckCopy(mp);
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

            CheckCopy(merklePatricia);
        }

        [TestMethod]
        public void Lista()
        {
            var lista = new[] {"oi", "oi1", "oi2", "oi12", "bola", "birosca123", "ca123", "oi123"};
            var mp = new MerklePatricia();
            foreach (var it in lista)
            {
                mp[it] = it;
                Console.WriteLine($"Linha: '{it}:{Encoding.UTF8.GetBytes(it).ByteToHexString(false, false)}':\n{mp}");
                Assert.AreEqual(it, mp[it]);
                Assert.IsTrue(mp.Validate());
            }

            CheckCopy(mp);
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

            CheckCopy(merklePatricia);
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

            CheckCopy(mp);
            CheckCopy(mp2);
        }

        [TestMethod]
        public void EqualsThree()
        {
            Assert.AreNotEqual(null, new MerklePatricia());
            Assert.AreNotEqual(new MerklePatricia(), new MerklePatricia {["oi"] = "oi"});

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

            CheckCopy(mpA);
            CheckCopy(mpB);
        }

        [TestMethod]
        public void ToStringTesting()
        {
            var mp = new MerklePatricia();
            Assert.AreEqual("{}", mp.ToString());
            mp["a"] = "a";
            var converted = Encoding.UTF8.GetBytes("a").ByteToHexString();
            Assert.AreEqual($"[\"{converted}\",\"{converted}\",\"{converted}\"]", mp.ToString());

            CheckCopy(mp);
        }

        [TestMethod]
        public void Equals()
        {
            var mpNode = new MerklePatricia();
            Assert.AreNotEqual(mpNode, null);
            Assert.IsFalse(mpNode == null);
            Assert.IsFalse(mpNode.Equals(null));

            Assert.AreEqual(mpNode, mpNode);
            Assert.IsTrue(mpNode == mpNode);
            Assert.IsTrue(mpNode.Equals(mpNode));

            var mpA = new MerklePatricia {["a"] = "a"};
            Assert.AreNotEqual(mpNode, mpA);
            Assert.IsFalse(mpNode == mpA);
            Assert.IsFalse(mpNode.Equals(mpA));

            CheckCopy(mpNode);
            CheckCopy(mpA);
        }
    }
}