// Copyright (C) 2015-2025 The Neo Project.
//
// UT_TestApplicationEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Build.Core.SmartContract;
using Neo.Build.Core.Tests.Helpers;
using Neo.VM;
using System.Linq;

namespace Neo.Build.Core.Tests.SmartContract
{
    [TestClass]
    public class UT_TestApplicationEngine
    {
        private readonly ILoggerFactory _loggerFactory;

        public UT_TestApplicationEngine()
        {
            _loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Disabled;
                });
            });
        }

        [TestMethod]
        public void Test()
        {
            var engine = new TestApplicationEngine(TestNode.BuildSettings, TestNode.NeoSystem.GetSnapshotCache(), _loggerFactory);

            using var sb = new ScriptBuilder()
                .EmitPush("Hello World!");

            engine.LoadScript(sb.ToArray());
            var state = engine.Execute();

            Assert.AreEqual(VMState.HALT, state);
            Assert.AreEqual(1, engine.ResultStack.Count);

            var stackItem = engine.ResultStack.First();
            var stackItemString = stackItem.GetString();

            Assert.AreEqual("Hello World!", stackItemString);
        }
    }
}
