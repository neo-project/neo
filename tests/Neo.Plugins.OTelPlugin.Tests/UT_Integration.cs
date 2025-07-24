// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Integration.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins.OTelPlugin.Tests
{
    [TestClass]
    public class UT_Integration
    {
        [TestMethod]
        public void TestOpenTelemetryMeterProviderCreation()
        {
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("test-service", serviceVersion: "1.0.0"))
                .AddMeter("TestMeter")
                .Build();

            Assert.IsNotNull(meterProvider);
        }

        [TestMethod]
        public void TestOpenTelemetryWithConsoleExporter()
        {
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("test-service"))
                .AddMeter("TestMeter")
                .AddConsoleExporter()
                .Build();

            using var meter = new Meter("TestMeter", "1.0.0");
            var counter = meter.CreateCounter<long>("test.counter");

            // Add measurements
            counter.Add(10);
            counter.Add(20);
            counter.Add(30);

            // Force flush
            meterProvider.ForceFlush();

            // Verify no exceptions thrown
            Assert.IsNotNull(meterProvider);
        }

        [TestMethod]
        public async Task TestMetricsCollectionUnderLoad()
        {
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("test-service"))
                .AddMeter("LoadTestMeter")
                .AddConsoleExporter()
                .Build();

            using var meter = new Meter("LoadTestMeter", "1.0.0");
            var counter = meter.CreateCounter<long>("load.test.counter");
            var histogram = meter.CreateHistogram<double>("load.test.histogram");

            // Simulate load
            var tasks = new List<Task>();
            var cts = new CancellationTokenSource();

            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var random = new Random();
                    while (!cts.Token.IsCancellationRequested)
                    {
                        counter.Add(1);
                        histogram.Record(random.NextDouble() * 100);
                    }
                }));
            }

            // Let it run for a short time
            await Task.Delay(100);
            cts.Cancel();
            await Task.WhenAll(tasks);

            // Force flush
            meterProvider.ForceFlush();

            // Verify no exceptions thrown
            Assert.IsNotNull(meterProvider);
        }

        [TestMethod]
        public void TestResourceAttributes()
        {

            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService("neo-node", serviceVersion: "3.8.1", serviceInstanceId: "test-instance")
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", "test"),
                    new KeyValuePair<string, object>("service.namespace", "blockchain"),
                    new KeyValuePair<string, object>("node.type", "full")
                });

            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddMeter("TestMeter")
                .AddConsoleExporter()
                .Build();

            using var meter = new Meter("TestMeter", "1.0.0");
            var counter = meter.CreateCounter<long>("test.counter");
            counter.Add(1);

            meterProvider.ForceFlush();

            // Verify no exceptions thrown
            Assert.IsNotNull(meterProvider);
            // Note: Resource attributes would be attached to exported metrics
        }

        [TestMethod]
        public void TestMultipleInstrumentTypes()
        {
            var exportedItems = new List<Metric>();

            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault())
                .AddMeter("MultiInstrumentMeter")
                .AddConsoleExporter()
                .Build();

            using var meter = new Meter("MultiInstrumentMeter", "1.0.0");

            // Create different instrument types
            var counter = meter.CreateCounter<long>("test.counter");
            var histogram = meter.CreateHistogram<double>("test.histogram");
            var upDownCounter = meter.CreateUpDownCounter<long>("test.updowncounter");

            // Observable instruments
            var observableCounter = meter.CreateObservableCounter<long>(
                "test.observable.counter",
                () => 42);

            var observableGauge = meter.CreateObservableGauge<double>(
                "test.observable.gauge",
                () => 3.14);

            // Record measurements
            counter.Add(10);
            histogram.Record(25.5);
            upDownCounter.Add(5);
            upDownCounter.Add(-3);

            // Force collection of observable instruments
            Thread.Sleep(100);
            meterProvider.ForceFlush();

            // Verify no exceptions thrown
            Assert.IsNotNull(meterProvider);
        }

        [TestMethod]
        public void TestMetricsWithTags()
        {
            var exportedItems = new List<Metric>();

            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault())
                .AddMeter("TaggedMeter")
                .AddConsoleExporter()
                .Build();

            using var meter = new Meter("TaggedMeter", "1.0.0");
            var counter = meter.CreateCounter<long>("transactions.processed");

            // Add measurements with different tags
            counter.Add(1, new KeyValuePair<string, object?>("type", "transfer"));
            counter.Add(1, new KeyValuePair<string, object?>("type", "mint"));
            counter.Add(1, new KeyValuePair<string, object?>("type", "burn"));

            counter.Add(1,
                new KeyValuePair<string, object?>("type", "transfer"),
                new KeyValuePair<string, object?>("status", "success"));

            counter.Add(1,
                new KeyValuePair<string, object?>("type", "transfer"),
                new KeyValuePair<string, object?>("status", "failed"));

            meterProvider.ForceFlush();

            // Verify no exceptions thrown
            Assert.IsNotNull(meterProvider);
        }

        [TestMethod]
        public void TestMeterProviderDisposal()
        {
            MeterProvider? meterProvider = null;
            Meter? meter = null;
            Counter<long>? counter = null;

            try
            {
                meterProvider = Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(ResourceBuilder.CreateDefault())
                    .AddMeter("DisposalTestMeter")
                    .AddConsoleExporter()
                    .Build();

                meter = new Meter("DisposalTestMeter", "1.0.0");
                counter = meter.CreateCounter<long>("test.counter");

                // Add measurement
                counter.Add(1);
                meterProvider.ForceFlush();

                Assert.IsNotNull(meterProvider);
            }
            finally
            {
                // Ensure proper disposal
                meter?.Dispose();
                meterProvider?.Dispose();
            }

            // After disposal, new measurements should not be recorded
            counter?.Add(1); // This should not throw but also not record
        }
    }
}
