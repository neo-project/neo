// Copyright (C) 2015-2025 The Neo Project.
//
// TestBlockchain.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.TestKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Persistence;
using Neo.Persistence.Providers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Neo.UnitTests
{
    public static class TestBlockchain
    {
        public static readonly NeoSystem TheNeoSystem;
        public static readonly UInt160[] DefaultExtensibleWitnessWhiteList;
        private static readonly MemoryStore Store = new();

        private class StoreProvider : IStoreProvider
        {
            public string Name => "TestProvider";

            public IStore GetStore(string path) => Store;
        }

        static TestBlockchain()
        {
            Console.WriteLine("initialize NeoSystem");
            TheNeoSystem = new NeoSystem(TestProtocolSettings.Default, new StoreProvider());
        }

        internal static void ResetStore()
        {
            Store.Reset();
            TheNeoSystem.Blockchain.Ask(new Blockchain.Initialize()).Wait();
        }

        internal static StoreCache GetTestSnapshotCache(bool reset = true)
        {
            if (reset)
                ResetStore();
            return TheNeoSystem.GetSnapshotCache();
        }
    }

    public class TestAssertProbe : ITestKitAssertions
    {
        public static TestAssertProbe Instance = new();

        private class TestComparer<T>(Func<T, T, bool> comparer) : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> _comparer = comparer;

            public bool Equals([DisallowNull] T x, [DisallowNull] T y) =>
                _comparer(x, y);

            public int GetHashCode([DisallowNull] T obj) =>
                obj.GetHashCode();
        }

        public void AssertEqual<T>(T expected, T actual, string format = "", params object[] args)
        {
            Assert.AreEqual(expected, actual, format, args);
        }

        public void AssertEqual<T>(T expected, T actual, Func<T, T, bool> comparer, string format = "", params object[] args)
        {
            Assert.AreEqual(expected, actual, new TestComparer<T>(comparer), format, args);
        }

        public void AssertFalse(bool condition, string format = "", params object[] args)
        {
            Assert.IsFalse(condition, format, args);
        }

        public void AssertTrue(bool condition, string format = "", params object[] args)
        {
            Assert.IsTrue(condition, format, args);
        }

        public void Fail(string format = "", params object[] args)
        {
            Assert.Fail(format, args);
        }
    }
}
