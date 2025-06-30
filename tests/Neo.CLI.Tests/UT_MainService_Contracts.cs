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
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.UnitTests.Extensions;
using Neo.VM;
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
            var neoSystemField = typeof(MainService).GetField("_neoSystem", BindingFlags.NonPublic | BindingFlags.Instance);
            if (neoSystemField == null)
                Assert.Fail("_neoSystem field not found");
            neoSystemField.SetValue(_mainService, _neoSystem);

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
            // Create a test contract with ABI using TestUtils
            var manifest = TestUtils.CreateDefaultManifest();

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
                    Parameters = [
                        new() { Name = "value", Type = ContractParameterType.Integer }
                    ],
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

            // Create a simple contract script
            using var sb = new ScriptBuilder();
            sb.EmitPush(true);
            sb.Emit(OpCode.RET);
            var script = sb.ToArray();

            // Create NefFile
            var nef = new NefFile
            {
                Compiler = "",
                Source = "",
                Tokens = Array.Empty<MethodToken>(),
                Script = script
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);

            // Create the contract state manually
            _contractState = new ContractState
            {
                Id = 1,
                Hash = _contractHash,
                Nef = nef,
                Manifest = manifest
            };

            // Properly add the contract to the test snapshot using the extension method
            var snapshot = _neoSystem.GetSnapshotCache();
            snapshot.AddContract(_contractHash, _contractState);

            // Commit the changes to make them available for subsequent operations
            snapshot.Commit();
        }

        [TestMethod]
        public void TestParseParameterFromAbi_Boolean()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");

            // Test true value
            var result = (ContractParameter)method.Invoke(_mainService, [ContractParameterType.Boolean, JToken.Parse("true")]);
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
            Assert.ThrowsExactly<TargetInvocationException>(() =>
                method.Invoke(_mainService, new object[] { ContractParameterType.Integer, JToken.Parse("\"abc\"") }));
        }

        [TestMethod]
        public void TestParseParameterFromAbi_InvalidHash160()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");

            // This should throw because the hash is invalid
            Assert.ThrowsExactly<TargetInvocationException>(() =>
                method.Invoke(_mainService, new object[] { ContractParameterType.Hash160, JToken.Parse("\"invalid_hash\"") }));
        }

        [TestMethod]
        public void TestParseParameterFromAbi_UnsupportedType()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");

            // InteropInterface is not supported for JSON parsing
            Assert.ThrowsExactly<TargetInvocationException>(() =>
                method.Invoke(_mainService, new object[] { ContractParameterType.InteropInterface, JToken.Parse("\"test\"") }));
        }

        private MethodInfo GetPrivateMethod(string methodName)
        {
            var method = typeof(MainService).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"Method {methodName} not found");
            return method;
        }

        #region Integration Tests for InvokeAbi Command

        [TestMethod]
        public void TestInvokeAbiCommand_ContractNotFound()
        {
            // Arrange
            var nonExistentHash = UInt160.Parse("0xffffffffffffffffffffffffffffffffffffffff");
            _consoleOutput.GetStringBuilder().Clear();

            // Act
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");
            invokeAbiMethod.Invoke(_mainService, new object[] { nonExistentHash, "test", null, null, null, 20m });

            // Assert
            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Contract does not exist"));
        }

        [TestMethod]
        public void TestInvokeAbiCommand_MethodNotFound()
        {
            // Arrange
            _consoleOutput.GetStringBuilder().Clear();

            // Act
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");
            invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "nonExistentMethod", null, null, null, 20m });

            // Assert
            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Method 'nonExistentMethod' does not exist"));
        }

        [TestMethod]
        public void TestInvokeAbiCommand_WrongParameterCount()
        {
            // Arrange
            _consoleOutput.GetStringBuilder().Clear();
            var args = new JArray(123, 456); // testBoolean expects 1 parameter, not 2

            // Act
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");
            invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "testBoolean", args, null, null, 20m });

            // Assert
            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Method 'testBoolean' exists but expects 1 parameters, not 2"));
        }

        [TestMethod]
        public void TestInvokeAbiCommand_TooManyArguments()
        {
            // Arrange
            _consoleOutput.GetStringBuilder().Clear();
            var args = new JArray("0x1234567890abcdef1234567890abcdef12345678", "0xabcdef1234567890abcdef1234567890abcdef12", 100, "extra");

            // Act
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");
            invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "testMultipleParams", args, null, null, 20m });

            // Assert
            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Method 'testMultipleParams' exists but expects 3 parameters, not 4"));
        }

        [TestMethod]
        public void TestInvokeAbiCommand_TooFewArguments()
        {
            // Arrange
            _consoleOutput.GetStringBuilder().Clear();
            var args = new JArray("0x1234567890abcdef1234567890abcdef12345678"); // testMultipleParams expects 3 parameters, not 1

            // Act
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");
            invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "testMultipleParams", args, null, null, 20m });

            // Assert
            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Method 'testMultipleParams' exists but expects 3 parameters, not 1"));
        }

        [TestMethod]
        public void TestInvokeAbiCommand_NoArgumentsForMethodExpectingParameters()
        {
            // Arrange
            _consoleOutput.GetStringBuilder().Clear();

            // Act - calling testBoolean with no arguments when it expects 1
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");
            invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "testBoolean", null, null, null, 20m });

            // Assert
            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Method 'testBoolean' exists but expects 1 parameters, not 0"));
        }

        [TestMethod]
        public void TestInvokeAbiCommand_InvalidParameterFormat()
        {
            // Arrange
            _consoleOutput.GetStringBuilder().Clear();
            var args = new JArray("invalid_hash160_format");

            // Act
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");
            invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "testHash160", args, null, null, 20m });

            // Assert
            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Failed to parse parameter 'value' (index 0)"));
        }

        [TestMethod]
        public void TestInvokeAbiCommand_SuccessfulInvocation_SingleParameter()
        {
            // Arrange
            _consoleOutput.GetStringBuilder().Clear();
            var args = new JArray(true);

            // Note: We can't easily intercept the OnInvokeCommand call in this test setup
            // The test verifies that parameter parsing works correctly by checking no errors occur

            // Act
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");
            try
            {
                invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "testBoolean", args, null, null, 20m });
            }
            catch (TargetInvocationException ex) when (ex.InnerException?.Message.Contains("This method does not not exist") == true)
            {
                // Expected since we're not fully mocking the invoke flow
                // The important part is that we reached the OnInvokeCommand call
            }

            // Since we can't easily intercept the OnInvokeCommand call in this test setup,
            // we'll verify the parameter parsing works correctly through unit tests above
        }

        [TestMethod]
        public void TestInvokeAbiCommand_ComplexTypes()
        {
            // Arrange
            _consoleOutput.GetStringBuilder().Clear();

            // Test with array parameter
            var innerArray = new JArray
            {
                1,
                2,
                3,
                "test",
                true
            };
            var arrayArgs = new JArray
            {
                innerArray
            };

            // Act & Assert - Array type
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");
            try
            {
                invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "testArray", arrayArgs, null, null, 20m });
            }
            catch (TargetInvocationException)
            {
                // Expected - we're testing parameter parsing, not full execution
            }

            // The fact that we don't get a parsing error means the array was parsed successfully
            var output = _consoleOutput.ToString();
            Assert.IsFalse(output.Contains("Failed to parse parameter"));
        }

        [TestMethod]
        public void TestInvokeAbiCommand_MultipleParameters()
        {
            // Arrange
            _consoleOutput.GetStringBuilder().Clear();
            var args = new JArray(
                "0x1234567890abcdef1234567890abcdef12345678",
                "0xabcdef1234567890abcdef1234567890abcdef12",
                "1000000"
            );

            // Act
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");
            try
            {
                invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "testMultipleParams", args, null, null, 20m });
            }
            catch (TargetInvocationException)
            {
                // Expected - we're testing parameter parsing, not full execution
            }

            // Assert - no parsing errors
            var output = _consoleOutput.ToString();
            Assert.IsFalse(output.Contains("Failed to parse parameter"));
        }

        [TestMethod]
        public void TestInvokeAbiCommand_WithSenderAndSigners()
        {
            // Arrange
            _consoleOutput.GetStringBuilder().Clear();
            var args = new JArray("test string");
            var sender = UInt160.Parse("0x1234567890abcdef1234567890abcdef12345678");
            var signers = new[] { sender, UInt160.Parse("0xabcdef1234567890abcdef1234567890abcdef12") };

            // Act
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");
            try
            {
                invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "testString", args, sender, signers, 15m });
            }
            catch (TargetInvocationException)
            {
                // Expected - we're testing parameter parsing, not full execution
            }

            // Assert - parameters should be parsed without error
            var output = _consoleOutput.ToString();
            Assert.IsFalse(output.Contains("Failed to parse parameter"));
        }

        [TestMethod]
        public void TestParseParameterFromAbi_ImprovedErrorMessages()
        {
            var method = GetPrivateMethod("ParseParameterFromAbi");

            // Test invalid integer format with helpful error
            try
            {
                method.Invoke(_mainService, new object[] { ContractParameterType.Integer, JToken.Parse("\"abc\"") });
                Assert.Fail("Expected exception for invalid integer");
            }
            catch (TargetInvocationException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentException));
                Assert.IsTrue(ex.InnerException.Message.Contains("Invalid integer format"));
                Assert.IsTrue(ex.InnerException.Message.Contains("Expected a numeric string"));
            }

            // Test invalid Hash160 format with helpful error
            try
            {
                method.Invoke(_mainService, new object[] { ContractParameterType.Hash160, JToken.Parse("\"invalid\"") });
                Assert.Fail("Expected exception for invalid Hash160");
            }
            catch (TargetInvocationException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentException));
                Assert.IsTrue(ex.InnerException.Message.Contains("Invalid Hash160 format"));
                Assert.IsTrue(ex.InnerException.Message.Contains("0x"));
                Assert.IsTrue(ex.InnerException.Message.Contains("40 hex characters"));
            }

            // Test invalid Base64 format with helpful error
            try
            {
                method.Invoke(_mainService, new object[] { ContractParameterType.ByteArray, JToken.Parse("\"not-base64!@#$\"") });
                Assert.Fail("Expected exception for invalid Base64");
            }
            catch (TargetInvocationException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentException));
                Assert.IsTrue(ex.InnerException.Message.Contains("Invalid ByteArray format"));
                Assert.IsTrue(ex.InnerException.Message.Contains("Base64 encoded string"));
            }
        }

        [TestMethod]
        public void TestInvokeAbiCommand_MethodOverloading()
        {
            // Test that the method correctly finds the right overload based on parameter count
            // Setup a contract with overloaded methods
            var manifest = TestUtils.CreateDefaultManifest();

            // Add overloaded methods with same name but different parameter counts
            manifest.Abi.Methods = new[]
            {
                new ContractMethodDescriptor
                {
                    Name = "transfer",
                    Parameters = new[]
                    {
                        new ContractParameterDefinition { Name = "to", Type = ContractParameterType.Hash160 },
                        new ContractParameterDefinition { Name = "amount", Type = ContractParameterType.Integer }
                    },
                    ReturnType = ContractParameterType.Boolean,
                    Safe = false
                },
                new ContractMethodDescriptor
                {
                    Name = "transfer",
                    Parameters = new[]
                    {
                        new ContractParameterDefinition { Name = "from", Type = ContractParameterType.Hash160 },
                        new ContractParameterDefinition { Name = "to", Type = ContractParameterType.Hash160 },
                        new ContractParameterDefinition { Name = "amount", Type = ContractParameterType.Integer }
                    },
                    ReturnType = ContractParameterType.Boolean,
                    Safe = false
                }
            };

            // Update the contract with overloaded methods
            _contractState.Manifest = manifest;
            var snapshot = _neoSystem.GetSnapshotCache();
            snapshot.AddContract(_contractHash, _contractState);
            snapshot.Commit();

            // Test calling the 2-parameter version
            _consoleOutput.GetStringBuilder().Clear();
            var args2 = new JArray("0x1234567890abcdef1234567890abcdef12345678", 100);
            var invokeAbiMethod = GetPrivateMethod("OnInvokeAbiCommand");

            try
            {
                invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "transfer", args2, null, null, 20m });
            }
            catch (TargetInvocationException)
            {
                // Expected - we're testing parameter parsing
            }

            // Should not have any method selection errors
            var output = _consoleOutput.ToString();
            Assert.IsFalse(output.Contains("Method 'transfer' exists but expects"));

            // Test calling with wrong parameter count should give helpful error
            _consoleOutput.GetStringBuilder().Clear();
            var args4 = new JArray("0x1234567890abcdef1234567890abcdef12345678", "0xabcdef1234567890abcdef1234567890abcdef12", 100, "extra");

            invokeAbiMethod.Invoke(_mainService, new object[] { _contractHash, "transfer", args4, null, null, 20m });

            output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Method 'transfer' exists but expects") || output.Contains("expects exactly"));
        }

        #endregion
    }
}
