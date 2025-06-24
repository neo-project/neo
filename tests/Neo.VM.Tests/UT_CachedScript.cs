// Copyright (C) 2015-2025 The Neo Project.
//
// UT_CachedScript.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Neo.VM.Tests
{
    [TestClass]
    public class UT_CachedScript
    {
        private byte[] GenerateSimpleScript()
        {
            var script = new List<byte>
            {
                // PUSH1
                (byte)OpCode.PUSH1,

                // PUSHDATA1 with 4 bytes
                (byte)OpCode.PUSHDATA1,
                4
            };
            script.AddRange(BitConverter.GetBytes(12345));

            // ADD
            script.Add((byte)OpCode.ADD);

            // RET
            script.Add((byte)OpCode.RET);

            return script.ToArray();
        }

        [TestMethod]
        public void TestBasicFunctionality()
        {
            var scriptBytes = GenerateSimpleScript();
            var cachedScript = new CachedScript(scriptBytes);
            var normalScript = new Script(scriptBytes);

            Assert.AreEqual(normalScript.Length, cachedScript.Length);

            // Test that both scripts return the same instructions
            for (int ip = 0; ip < scriptBytes.Length;)
            {
                var normalInstruction = normalScript.GetInstruction(ip);
                var cachedInstruction = cachedScript.GetInstruction(ip);

                Assert.AreEqual(normalInstruction.OpCode, cachedInstruction.OpCode);
                Assert.AreEqual(normalInstruction.Size, cachedInstruction.Size);

                ip += normalInstruction.Size;
            }
        }

        [TestMethod]
        public void TestCachePreDecoding()
        {
            var scriptBytes = GenerateSimpleScript();
            var cachedScript = new CachedScript(scriptBytes, cacheThreshold: 1000);

            // For small scripts, all instructions should be pre-decoded
            var (cached, total, hitRatio) = cachedScript.GetCacheStats();

            Assert.IsTrue(hitRatio > 0, "Some instructions should be cached");
            Assert.AreEqual(scriptBytes.Length, total);
        }

        [TestMethod]
        public void TestLargeScriptNoPreDecoding()
        {
            var scriptBytes = GenerateSimpleScript();
            var cachedScript = new CachedScript(scriptBytes, cacheThreshold: 1); // Very low threshold

            // Initially, no instructions should be cached
            var (cached, total, hitRatio) = cachedScript.GetCacheStats();
            Assert.AreEqual(0, cached);
            Assert.AreEqual(scriptBytes.Length, total);
            Assert.AreEqual(0.0, hitRatio);

            // After accessing an instruction, it should be cached
            _ = cachedScript.GetInstruction(0);
            var (cachedAfter, totalAfter, hitRatioAfter) = cachedScript.GetCacheStats();
            Assert.IsTrue(cachedAfter > 0);
        }

        [TestMethod]
        public void TestRepeatedAccess()
        {
            var scriptBytes = GenerateSimpleScript();
            var cachedScript = new CachedScript(scriptBytes);

            // Access the same instruction multiple times
            var instruction1 = cachedScript.GetInstruction(0);
            var instruction2 = cachedScript.GetInstruction(0);
            var instruction3 = cachedScript.GetInstruction(0);

            // Should return the same instance (from cache)
            Assert.AreEqual(instruction1.OpCode, instruction2.OpCode);
            Assert.AreEqual(instruction2.OpCode, instruction3.OpCode);
        }

        [TestMethod]
        public void TestClearCache()
        {
            var scriptBytes = GenerateSimpleScript();
            var cachedScript = new CachedScript(scriptBytes);

            // Access an instruction to cache it
            _ = cachedScript.GetInstruction(0);
            var (cachedBefore, _, _) = cachedScript.GetCacheStats();
            Assert.IsTrue(cachedBefore > 0);

            // Clear the cache
            cachedScript.ClearCache();
            var (cachedAfter, _, _) = cachedScript.GetCacheStats();
            Assert.AreEqual(0, cachedAfter);
        }

        [TestMethod]
        public void TestGetCacheHitRatio()
        {
            var scriptBytes = GenerateSimpleScript();
            var cachedScript = new CachedScript(scriptBytes);

            // Should have high hit ratio for small scripts (pre-decoded)
            var hitRatio = cachedScript.GetCacheHitRatio();
            Assert.IsTrue(hitRatio >= 0.0 && hitRatio <= 100.0);
        }

        [TestMethod]
        public void TestStrictMode()
        {
            var scriptBytes = GenerateSimpleScript();

            // Should work with strict mode enabled
            var cachedScript = new CachedScript(scriptBytes, strictMode: true);
            var normalScript = new Script(scriptBytes, strictMode: true);

            Assert.AreEqual(normalScript.Length, cachedScript.Length);

            // Instructions should be identical
            var normalInstruction = normalScript.GetInstruction(0);
            var cachedInstruction = cachedScript.GetInstruction(0);
            Assert.AreEqual(normalInstruction.OpCode, cachedInstruction.OpCode);
        }

        [TestMethod]
        public void TestInvalidInstructionPosition()
        {
            var scriptBytes = GenerateSimpleScript();
            var cachedScript = new CachedScript(scriptBytes);

            // Should throw for invalid positions
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => cachedScript.GetInstruction(-1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => cachedScript.GetInstruction(scriptBytes.Length));
        }

        [TestMethod]
        public void TestEmptyScript()
        {
            var emptyScript = new byte[0];
            var cachedScript = new CachedScript(emptyScript);

            Assert.AreEqual(0, cachedScript.Length);
            Assert.AreEqual(100.0, cachedScript.GetCacheHitRatio()); // Empty script = 100% hit ratio

            var (cached, total, hitRatio) = cachedScript.GetCacheStats();
            Assert.AreEqual(0, cached);
            Assert.AreEqual(0, total);
            Assert.AreEqual(100.0, hitRatio);
        }

        [TestMethod]
        public void TestCacheThreshold()
        {
            var scriptBytes = GenerateSimpleScript();

            // Script below threshold should be pre-decoded
            var cachedScript1 = new CachedScript(scriptBytes, cacheThreshold: 1000);
            var hitRatio1 = cachedScript1.GetCacheHitRatio();

            // Script above threshold should not be pre-decoded
            var cachedScript2 = new CachedScript(scriptBytes, cacheThreshold: 1);
            var hitRatio2 = cachedScript2.GetCacheHitRatio();

            Assert.IsTrue(hitRatio1 >= hitRatio2, "Lower threshold should result in higher cache hit ratio");
        }
    }
}
