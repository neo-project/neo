using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_StdLib
    {
        [TestMethod]
        public void TestBinary()
        {
            var data = System.Array.Empty<byte>();

            CollectionAssert.AreEqual(data, StdLib.Base64Decode(StdLib.Base64Encode(data)));
            CollectionAssert.AreEqual(data, StdLib.Base58Decode(StdLib.Base58Encode(data)));

            data = new byte[] { 1, 2, 3 };

            CollectionAssert.AreEqual(data, StdLib.Base64Decode(StdLib.Base64Encode(data)));
            CollectionAssert.AreEqual(data, StdLib.Base58Decode(StdLib.Base58Encode(data)));
            Assert.AreEqual("AQIDBA==", StdLib.Base64Encode(new byte[] { 1, 2, 3, 4 }));
            Assert.AreEqual("2VfUX", StdLib.Base58Encode(new byte[] { 1, 2, 3, 4 }));
        }

        [TestMethod]
        public void TestItoaAtoi()
        {
            Assert.AreEqual("1", StdLib.Itoa(BigInteger.One, 10));
            Assert.AreEqual("1", StdLib.Itoa(BigInteger.One, 16));
            Assert.AreEqual("-1", StdLib.Itoa(BigInteger.MinusOne, 10));
            Assert.AreEqual("f", StdLib.Itoa(BigInteger.MinusOne, 16));
            Assert.AreEqual("3b9aca00", StdLib.Itoa(1_000_000_000, 16));
            Assert.AreEqual(-1, StdLib.Atoi("-1", 10));
            Assert.AreEqual(1, StdLib.Atoi("+1", 10));
            Assert.AreEqual(-1, StdLib.Atoi("ff", 16));
            Assert.AreEqual(-1, StdLib.Atoi("FF", 16));
            Assert.ThrowsException<System.FormatException>(() => StdLib.Atoi("a", 10));
            Assert.ThrowsException<System.FormatException>(() => StdLib.Atoi("g", 16));
            Assert.ThrowsException<System.ArgumentOutOfRangeException>(() => StdLib.Atoi("a", 11));
        }

        [TestMethod]
        public void MemoryCompare()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memoryCompare", "abc", "c");
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memoryCompare", "abc", "d");
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memoryCompare", "abc", "abc");
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memoryCompare", "abc", "abcd");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(4, engine.ResultStack.Count);

                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(0, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
            }
        }

        [TestMethod]
        public void CheckDecodeEncode()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using (ScriptBuilder script = new())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "base58CheckEncode", new byte[] { 1, 2, 3 });

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);

                Assert.AreEqual("3DUz7ncyT", engine.ResultStack.Pop<ByteString>().GetString());
            }

            using (ScriptBuilder script = new())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "base58CheckDecode", "3DUz7ncyT");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);

                CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, engine.ResultStack.Pop<ByteString>().GetSpan().ToArray());
            }

            // Error

            using (ScriptBuilder script = new())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "base58CheckDecode", "AA");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.FAULT);
            }

            using (ScriptBuilder script = new())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "base58CheckDecode", null);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.FAULT);
            }
        }

        [TestMethod]
        public void MemorySearch()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 0);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 1);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 2);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 3);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "d", 0);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(5, engine.ResultStack.Count);

                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(2, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(2, engine.ResultStack.Pop<Integer>().GetInteger());
            }

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 0, false);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 1, false);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 2, false);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 3, false);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "d", 0, false);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(5, engine.ResultStack.Count);

                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(2, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(2, engine.ResultStack.Pop<Integer>().GetInteger());
            }

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 0, true);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 1, true);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 2, true);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 3, true);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "d", 0, true);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(5, engine.ResultStack.Count);

                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(2, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
            }
        }

        [TestMethod]
        public void StringSplit()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "stringSplit", "a,b", ",");

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(1, engine.ResultStack.Count);

            var arr = engine.ResultStack.Pop<Array>();
            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual("a", arr[0].GetString());
            Assert.AreEqual("b", arr[1].GetString());
        }

        [TestMethod]
        public void StringElementLength()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "strLen", "🦆");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "strLen", "ã");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "strLen", "a");

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(3, engine.ResultStack.Count);
            Assert.AreEqual(1, engine.ResultStack.Pop().GetInteger());
            Assert.AreEqual(1, engine.ResultStack.Pop().GetInteger());
            Assert.AreEqual(1, engine.ResultStack.Pop().GetInteger());
        }

        [TestMethod]
        public void TestInvalidUtf8Sequence()
        {
            // Simulating invalid UTF-8 byte (0xff) decoded as a UTF-16 char
            const char badChar = (char)0xff;
            var badStr = badChar.ToString();
            var snapshot = TestBlockchain.GetTestSnapshot();

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "strLen", badStr);
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "strLen", badStr + "ab");

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(2, engine.ResultStack.Count);
            Assert.AreEqual(3, engine.ResultStack.Pop().GetInteger());
            Assert.AreEqual(1, engine.ResultStack.Pop().GetInteger());
        }

        [TestMethod]
        public void Json_Deserialize()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            // Good

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonDeserialize", "123");
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonDeserialize", "null");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(2, engine.ResultStack.Count);

                engine.ResultStack.Pop<Null>();
                Assert.IsTrue(engine.ResultStack.Pop().GetInteger() == 123);
            }

            // Error 1 - Wrong Json

            using (ScriptBuilder script = new())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonDeserialize", "***");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.FAULT);
                Assert.AreEqual(0, engine.ResultStack.Count);
            }

            // Error 2 - No decimals

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonDeserialize", "123.45");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.FAULT);
                Assert.AreEqual(0, engine.ResultStack.Count);
            }
        }

        [TestMethod]
        public void Json_Serialize()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            // Good

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize", 5);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize", true);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize", "test");
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize", new object[] { null });
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize", new ContractParameter(ContractParameterType.Map)
                {
                    Value = new List<KeyValuePair<ContractParameter, ContractParameter>>() {
                        { new KeyValuePair<ContractParameter, ContractParameter>(
                            new ContractParameter(ContractParameterType.String){ Value="key" },
                            new ContractParameter(ContractParameterType.String){ Value= "value" })
                        }
                    }
                });

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(5, engine.ResultStack.Count);

                Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "{\"key\":\"value\"}");
                Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "null");
                Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "\"test\"");
                Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "true");
                Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "5");
            }

            // Error

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.FAULT);
                Assert.AreEqual(0, engine.ResultStack.Count);
            }
        }

        [TestMethod]
        public void TestRuntime_Serialize()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            // Good

            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "serialize", 100);
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "serialize", "test");

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(2, engine.ResultStack.Count);

            Assert.AreEqual(engine.ResultStack.Pop<ByteString>().GetSpan().ToHexString(), "280474657374");
            Assert.AreEqual(engine.ResultStack.Pop<ByteString>().GetSpan().ToHexString(), "210164");
        }

        [TestMethod]
        public void TestRuntime_Deserialize()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            // Good

            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "deserialize", "280474657374".HexToBytes());
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "deserialize", "210164".HexToBytes());

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(2, engine.ResultStack.Count);

            Assert.AreEqual(engine.ResultStack.Pop<Integer>().GetInteger(), 100);
            Assert.AreEqual(engine.ResultStack.Pop<ByteString>().GetString(), "test");
        }

        [TestMethod]
        public void TestRegex()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using ScriptBuilder script = new();

            // 1. Email address
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "contact@neo.org", @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "contact@neo", @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");

            // 2. URL with specific constraints (Matches http or https URLs)
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "https://www.example.com", @"^https?:\/\/([a-z0-9]+[.])*[a-z0-9]+\.[a-z]+(\/[^\s]*)?$");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "www.example.com", @"^https?:\/\/([a-z0-9]+[.])*[a-z0-9]+\.[a-z]+(\/[^\s]*)?$");

            // 3. Credit card number format (16 digits, can have spaces or dashes)
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "1234-5678-9012-3456", @"^(\d{4}-){3}\d{4}$|^(\d{4} ){3}\d{4}$|^\d{16}$");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "1234567890123456", @"^(\d{4}-){3}\d{4}$|^(\d{4} ){3}\d{4}$|^\d{16}$");

            // 4. Dates in a specific format (dd/mm/yyyy)
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "03/10/2023", @"^(0[1-9]|1[0-9]|2[0-9]|3[01])\/(0[1-9]|1[0-2])\/\d{4}$");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "03-10-2023", @"^(0[1-9]|1[0-9]|2[0-9]|3[01])\/(0[1-9]|1[0-2])\/\d{4}$");

            // 5. Passwords with certain strength requirements (At least 8 characters, 1 uppercase, 1 lowercase, 1 number)
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "Passw0rd", @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{8,}$");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "password", @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{8,}$");

            // 6. Bitcoin Address
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2", @"^[13][a-km-zA-HJ-NP-Z1-9]{25,34}$");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "1BvBMSE", @"^[13][a-km-zA-HJ-NP-Z1-9]{25,34}$");

            // 7. Ethereum Address
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "0x5ed8cee6b63b1c6afce3ad7c92f4fd7e1b8fad9f", @"^0x[a-fA-F0-9]{40}$");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "0x5ed8cee6b63b1c6afce", @"^0x[a-fA-F0-9]{40}$");

            // 8. Transaction ID (or hash)
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "5e2c3b8c3a862cf9e1d3e1b8c77b3323c68b4512db27d9a72c99106a8f2a8a7d", @"^[a-fA-F0-9]{64}$");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "5e2c3b8c3a862cf9", @"^[a-fA-F0-9]{64}$");

            // 9. Block Hash
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "6e3b34e4ea1d9f77c061a741c3c5d0a2a1f25e1d5e2a3332c0cd0c7c0a2b5d2e", @"^[a-fA-F0-9]{64}$");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "6e3b34e4ea1d9f77", @"^[a-fA-F0-9]{64}$");

            // 10. Neo N3 Address
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "Ndvb2h3qR4jQtR4t8keNBDmAm9BzmTtmwN", @"^N[123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]{33}$");
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", "Ndvb2h3qR4jQtR4", @"^N[123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]{33}$");

            var worstCaseStr = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaab";

            // 11. Worst case scenario
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "regex", worstCaseStr, @"^(a?)*a$");

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(21, engine.ResultStack.Count);

            // Assert for each test case (assuming the order remains same)
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean()); // Worst case
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean()); // Neo N3 Address
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());  // Neo N3 Address
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean()); // Block Hash
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());  // Block Hash
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean()); // Transaction ID (or hash)
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());  // Transaction ID (or hash)
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean()); // Ethereum Address
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());  // Ethereum Address
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean()); // Bitcoin Address
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());  // Bitcoin Address
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean()); // password
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());  // Passw0rd
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean()); // 03-10-2023
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());  // 03/10/2023
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());  // 1234567890123456
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());  // 1234-5678-9012-3456
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean()); // www.example.com
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());  // https://www.example.com
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean()); // contact@neo
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());  // contact@neo.org
        }

    }
}
