// Copyright (C) 2015-2025 The Neo Project.
//
// UT_MainService_Contracts.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.CLI;
using Neo.ConsoleService;
using Neo.Cryptography.ECC;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Neo.CLI.Tests
{
    [TestClass]
    public class UT_MainService_Contracts
    {
        private MainService _mainService;
        private NeoSystem _neoSystem;
        private Mock<Wallet> _mockWallet;
        private UInt160 _contractHash;
        private ContractState _contractState;
        private StringWriter _consoleOutput;
        private TextWriter _originalOutput;

        [TestInitialize]
        public void TestSetup()
        {
            _originalOutput = Console.Out;
            _consoleOutput = new StringWriter();
            Console.SetOut(_consoleOutput);

            // Initialize TestBlockchain
            _neoSystem = TestBlockchain.GetSystem();
            
            // Create MainService instance
            _mainService = new MainService();
            
            // Set NeoSystem using reflection
            var neoSystemField = typeof(MainService).GetField("NeoSystem", BindingFlags.NonPublic | BindingFlags.Static);
            neoSystemField?.SetValue(null, _neoSystem);

            // Setup mock wallet
            _mockWallet = new Mock<Wallet>();
            var mockAccount = new Mock<WalletAccount>(UInt160.Zero, null);
            _mockWallet.Setup(w => w.GetDefaultAccount()).Returns(mockAccount.Object);
            
            // Set CurrentWallet using reflection
            var walletField = typeof(MainService).GetField("CurrentWallet", BindingFlags.NonPublic | BindingFlags.Instance);
            walletField?.SetValue(_mainService, _mockWallet.Object);

            // Setup test contract
            _contractHash = UInt160.Parse("0x1234567890abcdef1234567890abcdef12345678");
            SetupTestContract();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Console.SetOut(_originalOutput);
            _consoleOutput.Dispose();
        }

        private void SetupTestContract()
        {
            // Create a test contract with ABI
            var manifest = new ContractManifest
            {
                Name = "TestContract",
                Abi = new ContractAbi()
            };

            // Add test methods with different parameter types
            var methods = new List<ContractMethodDescriptor>
            {
                new ContractMethodDescriptor
                {
                    Name = "testBoolean",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition { Name = "value", Type = ContractParameterType.Boolean }
                    },
                    ReturnType = ContractParameterType.Boolean,
                    Safe = true
                },
                new ContractMethodDescriptor
                {
                    Name = "testInteger",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition { Name = "value", Type = ContractParameterType.Integer }
                    },
                    ReturnType = ContractParameterType.Integer,
                    Safe = true
                },
                new ContractMethodDescriptor
                {
                    Name = "testString",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition { Name = "value", Type = ContractParameterType.String }
                    },
                    ReturnType = ContractParameterType.String,
                    Safe = true
                },
                new ContractMethodDescriptor
                {
                    Name = "testHash160",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition { Name = "value", Type = ContractParameterType.Hash160 }
                    },
                    ReturnType = ContractParameterType.Hash160,
                    Safe = true
                },
                new ContractMethodDescriptor
                {
                    Name = "testArray",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition { Name = "values", Type = ContractParameterType.Array }
                    },
                    ReturnType = ContractParameterType.Array,
                    Safe = true
                },
                new ContractMethodDescriptor
                {
                    Name = "testMultipleParams",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition { Name = "from", Type = ContractParameterType.Hash160 },
                        new ContractParameterDefinition { Name = "to", Type = ContractParameterType.Hash160 },
                        new ContractParameterDefinition { Name = "amount", Type = ContractParameterType.Integer }
                    },
                    ReturnType = ContractParameterType.Boolean,
                    Safe = false
                }
            };

            manifest.Abi.Methods = methods.ToArray();

            _contractState = new ContractState
            {
                Id = 1,
                Hash = _contractHash,
                Nef = new NefFile { Script = new byte[] { 0x01 } },
                Manifest = manifest
            };

            // Mock ContractManagement to return our test contract
            var snapshot = _neoSystem.StoreView;
            var contractManagement = new Mock<ContractManagement>();
            
            // This is a simplified approach - in reality you might need more complex mocking
            // For the purpose of this test, we'll use reflection to access the private methods
        }

        [TestMethod]
        public void TestParseParameterFromAbi_Boolean()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            // Test true value
            var result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Boolean, JToken.Parse("true") });
            Assert.AreEqual(ContractParameterType.Boolean, result.Type);
            Assert.AreEqual(true, result.Value);

            // Test false value
            result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Boolean, JToken.Parse("false") });
            Assert.AreEqual(ContractParameterType.Boolean, result.Type);
            Assert.AreEqual(false, result.Value);
        }

        [TestMethod]
        public void TestParseParameterFromAbi_Integer()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            // Test positive integer
            var result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Integer, JToken.Parse("\"123\"") });
            Assert.AreEqual(ContractParameterType.Integer, result.Type);
            Assert.AreEqual(new BigInteger(123), result.Value);

            // Test negative integer
            result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Integer, JToken.Parse("\"-456\"") });
            Assert.AreEqual(ContractParameterType.Integer, result.Type);
            Assert.AreEqual(new BigInteger(-456), result.Value);

            // Test large integer
            result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Integer, JToken.Parse("\"999999999999999999999\"") });
            Assert.AreEqual(ContractParameterType.Integer, result.Type);
            Assert.AreEqual(BigInteger.Parse("999999999999999999999"), result.Value);
        }

        [TestMethod]
        public void TestParseParameterFromAbi_String()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            var result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.String, JToken.Parse("\"Hello, World!\"") });
            Assert.AreEqual(ContractParameterType.String, result.Type);
            Assert.AreEqual("Hello, World!", result.Value);

            // Test empty string
            result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.String, JToken.Parse("\"\"") });
            Assert.AreEqual(ContractParameterType.String, result.Type);
            Assert.AreEqual("", result.Value);
        }

        [TestMethod]
        public void TestParseParameterFromAbi_Hash160()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            var hash160 = "0x1234567890abcdef1234567890abcdef12345678";
            var result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Hash160, JToken.Parse($"\"{hash160}\"") });
            Assert.AreEqual(ContractParameterType.Hash160, result.Type);
            Assert.AreEqual(UInt160.Parse(hash160), result.Value);
        }

        [TestMethod]
        public void TestParseParameterFromAbi_ByteArray()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            var base64 = Convert.ToBase64String(new byte[] { 0x01, 0x02, 0x03, 0x04 });
            var result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.ByteArray, JToken.Parse($"\"{base64}\"") });
            Assert.AreEqual(ContractParameterType.ByteArray, result.Type);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, (byte[])result.Value);
        }

        [TestMethod]
        public void TestParseParameterFromAbi_Array()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            var arrayJson = "[1, \"hello\", true]";
            var result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Array, JToken.Parse(arrayJson) });
            Assert.AreEqual(ContractParameterType.Array, result.Type);
            
            var array = (ContractParameter[])result.Value;
            Assert.AreEqual(3, array.Length);
            Assert.AreEqual(ContractParameterType.Integer, array[0].Type);
            Assert.AreEqual(ContractParameterType.String, array[1].Type);
            Assert.AreEqual(ContractParameterType.Boolean, array[2].Type);
        }

        [TestMethod]
        public void TestParseParameterFromAbi_Map()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            var mapJson = "{\"key1\": \"value1\", \"key2\": 123}";
            var result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Map, JToken.Parse(mapJson) });
            Assert.AreEqual(ContractParameterType.Map, result.Type);
            
            var map = (List<KeyValuePair<ContractParameter, ContractParameter>>)result.Value;
            Assert.AreEqual(2, map.Count);
            Assert.AreEqual("key1", map[0].Key.Value);
            Assert.AreEqual("value1", map[0].Value.Value);
            Assert.AreEqual("key2", map[1].Key.Value);
            Assert.AreEqual(new BigInteger(123), map[1].Value.Value);
        }

        [TestMethod]
        public void TestParseParameterFromAbi_Any()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            // Test Any with boolean
            var result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Any, JToken.Parse("true") });
            Assert.AreEqual(ContractParameterType.Boolean, result.Type);
            Assert.AreEqual(true, result.Value);

            // Test Any with integer
            result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Any, JToken.Parse("123") });
            Assert.AreEqual(ContractParameterType.Integer, result.Type);
            Assert.AreEqual(new BigInteger(123), result.Value);

            // Test Any with string
            result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Any, JToken.Parse("\"test\"") });
            Assert.AreEqual(ContractParameterType.String, result.Type);
            Assert.AreEqual("test", result.Value);

            // Test Any with array
            result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.Any, JToken.Parse("[1, 2, 3]") });
            Assert.AreEqual(ContractParameterType.Array, result.Type);
            Assert.AreEqual(3, ((ContractParameter[])result.Value).Length);
        }

        [TestMethod]
        public void TestParseParameterFromAbi_Null()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            var result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.String, null });
            Assert.AreEqual(ContractParameterType.String, result.Type);
            Assert.IsNull(result.Value);

            result = (ContractParameter)method.Invoke(_mainService, new object[] { ContractParameterType.String, JToken.Null });
            Assert.AreEqual(ContractParameterType.String, result.Type);
            Assert.IsNull(result.Value);
        }

        [TestMethod]
        public void TestParseParameterFromAbi_InvalidInteger()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            // This should throw because "abc" is not a valid integer
            Assert.ThrowsException<TargetInvocationException>(() =>
                method.Invoke(_mainService, new object[] { ContractParameterType.Integer, JToken.Parse("\"abc\"") }));
        }

        [TestMethod]
        public void TestParseParameterFromAbi_InvalidHash160()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            // This should throw because the hash is invalid
            Assert.ThrowsException<TargetInvocationException>(() =>
                method.Invoke(_mainService, new object[] { ContractParameterType.Hash160, JToken.Parse("\"invalid_hash\"") }));
        }

        [TestMethod]
        public void TestParseParameterFromAbi_UnsupportedType()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");
            
            // InteropInterface is not supported for JSON parsing
            Assert.ThrowsException<TargetInvocationException>(() =>
                method.Invoke(_mainService, new object[] { ContractParameterType.InteropInterface, JToken.Parse("\"test\"") }));
        }

        private MethodInfo GetPrivateMethod(string methodName)
        {
            var method = typeof(MainService).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"Method {methodName} not found");
            return method;
        }
    }
}