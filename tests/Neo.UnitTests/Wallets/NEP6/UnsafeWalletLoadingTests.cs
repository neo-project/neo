// Copyright (C) 2015-2024 The Neo Project.
//
// UnsafeWalletLoadingTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Neo.UnitTests.Wallets.NEP6
{
    public class UnsafeWalletLoadingTests
    {
        private class UnsafeWalletContainer
        {
            public volatile UnsafeWallet Wallet;

            public void LoadWallet()
            {
                Wallet = new UnsafeWallet();
            }
        }

        private class UnsafeWallet
        {
            public int Field1;
            public int Field2;
            public int Field3;
            public int Field4;

            public UnsafeWallet()
            {
                Field1 = 1;
                Thread.Sleep(1); // Increased delay and using Sleep instead of SpinWait
                Field2 = 2;
                Thread.Sleep(1);
                Field3 = 3;
                Thread.Sleep(1);
                Field4 = 4;
            }

            public bool IsFullyConstructed => Field1 == 1 && Field2 == 2 && Field3 == 3 && Field4 == 4;
        }

        [Fact]
        public void TestPartiallyConstructedWalletObservation()
        {
            const int iterations = 100000;
            int partiallyConstructedObservations = 0;

            Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, i =>
            {
                var container = new UnsafeWalletContainer();

                Thread loadThread = new Thread(() =>
                {
                    container.LoadWallet();
                });

                Thread checkThread = new Thread(() =>
                {
                    while (container.Wallet == null) { Thread.Yield(); }
                    if (!container.Wallet.IsFullyConstructed)
                    {
                        Interlocked.Increment(ref partiallyConstructedObservations);
                    }
                });

                loadThread.Start();
                checkThread.Start();

                loadThread.Join();
                checkThread.Join();
            });

            Console.WriteLine($"Partially constructed observations: {partiallyConstructedObservations} out of {iterations}");
            Assert.True(partiallyConstructedObservations > 0, "No partially constructed wallet was observed. This doesn't mean the issue doesn't exist, just that it wasn't reproduced in this run.");
        }
    }
}
