// Copyright (C) 2015-2025 The Neo Project.
//
// UT_MetricsCollection.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.OpenTelemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;

namespace Neo.Plugins.OTelPlugin.Tests
{
    [TestClass]
    public class UT_MetricsCollection
    {
        private List<KeyValuePair<string, object?>> _recordedMeasurements = null!;
        private MeterListener? _meterListener;

        [TestInitialize]
        public void Setup()
        {
            _recordedMeasurements = new List<KeyValuePair<string, object?>>();
            _meterListener = new MeterListener();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _meterListener?.Dispose();
        }

        [TestMethod]
        public void TestMeterCreation()
        {
            var meterName = "Neo.Blockchain";
            var meterVersion = "1.0.0";

            using var meter = new Meter(meterName, meterVersion);

            Assert.IsNotNull(meter);
            Assert.AreEqual(meterName, meter.Name);
            Assert.AreEqual(meterVersion, meter.Version);
        }

        [TestMethod]
        public void TestCounterCreation()
        {
            using var meter = new Meter("Neo.Blockchain", "1.0.0");
            var counter = meter.CreateCounter<long>("neo.requests", "requests", "Total number of requests");

            Assert.IsNotNull(counter);
            Assert.AreEqual("neo.requests", counter.Name);
            Assert.AreEqual("requests", counter.Unit);
            Assert.AreEqual("Total number of requests", counter.Description);
        }

        [TestMethod]
        public void TestCounterIncrement()
        {
            using var meter = new Meter("Neo.Blockchain", "1.0.0");
            var counter = meter.CreateCounter<long>("neo.test.counter");

            _meterListener!.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Neo.Blockchain")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _meterListener!.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                _recordedMeasurements.Add(new KeyValuePair<string, object?>(instrument.Name, measurement));
            });

            _meterListener!.Start();

            // Increment counter
            counter.Add(1);
            counter.Add(5);
            counter.Add(10);

            // Give time for measurements to be recorded
            Thread.Sleep(100);

            Assert.AreEqual(3, _recordedMeasurements.Count);
            Assert.AreEqual("neo.test.counter", _recordedMeasurements[0].Key);
            Assert.AreEqual(1L, _recordedMeasurements[0].Value);
            Assert.AreEqual(5L, _recordedMeasurements[1].Value);
            Assert.AreEqual(10L, _recordedMeasurements[2].Value);
        }

        [TestMethod]
        public void TestHistogramCreation()
        {
            using var meter = new Meter("Neo.Blockchain", "1.0.0");
            var histogram = meter.CreateHistogram<double>(
                "neo.block.processing_time",
                "ms",
                "Time taken to process a block");

            Assert.IsNotNull(histogram);
            Assert.AreEqual("neo.block.processing_time", histogram.Name);
            Assert.AreEqual("ms", histogram.Unit);
        }

        [TestMethod]
        public void TestHistogramRecording()
        {
            using var meter = new Meter("Neo.Blockchain", "1.0.0");
            var histogram = meter.CreateHistogram<double>("neo.test.histogram");

            _meterListener!.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Neo.Blockchain")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _meterListener!.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                _recordedMeasurements.Add(new KeyValuePair<string, object?>(instrument.Name, measurement));
            });

            _meterListener!.Start();

            // Record values
            histogram.Record(10.5);
            histogram.Record(25.3);
            histogram.Record(100.7);

            // Give time for measurements to be recorded
            Thread.Sleep(100);

            Assert.AreEqual(3, _recordedMeasurements.Count);
            Assert.AreEqual(10.5, _recordedMeasurements[0].Value);
            Assert.AreEqual(25.3, _recordedMeasurements[1].Value);
            Assert.AreEqual(100.7, _recordedMeasurements[2].Value);
        }

        [TestMethod]
        public void TestObservableGaugeCreation()
        {
            using var meter = new Meter("Neo.Blockchain", "1.0.0");
            var currentHeight = 12345L;

            var gauge = meter.CreateObservableGauge<long>(
                "neo.blockchain.height",
                () => currentHeight,
                "blocks",
                "Current blockchain height");

            Assert.IsNotNull(gauge);
            Assert.AreEqual("neo.blockchain.height", gauge.Name);
        }

        [TestMethod]
        public void TestCounterWithTags()
        {
            using var meter = new Meter("Neo.Blockchain", "1.0.0");
            var counter = meter.CreateCounter<long>("neo.transactions");

            var recordedTagsCount = 0;

            _meterListener!.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Neo.Blockchain")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _meterListener!.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                _recordedMeasurements.Add(new KeyValuePair<string, object?>(instrument.Name, measurement));
                recordedTagsCount++;
            });

            _meterListener!.Start();

            // Add with tags
            var tags = new KeyValuePair<string, object?>[]
            {
                new("type", "transfer"),
                new("status", "success")
            };

            counter.Add(1, tags);

            Thread.Sleep(100);

            Assert.AreEqual(1, _recordedMeasurements.Count);
            Assert.AreEqual(1L, _recordedMeasurements[0].Value);
            Assert.AreEqual(1, recordedTagsCount);
        }

        [TestMethod]
        public void TestMultipleMetersIsolation()
        {
            using var meter1 = new Meter("Neo.Blockchain", "1.0.0");
            using var meter2 = new Meter("Neo.Network", "1.0.0");

            var counter1 = meter1.CreateCounter<long>("counter1");
            var counter2 = meter2.CreateCounter<long>("counter2");

            var meter1Measurements = new List<long>();
            var meter2Measurements = new List<long>();

            _meterListener!.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Neo.Blockchain" || instrument.Meter.Name == "Neo.Network")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _meterListener!.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                if (instrument.Meter.Name == "Neo.Blockchain")
                    meter1Measurements.Add(measurement);
                else if (instrument.Meter.Name == "Neo.Network")
                    meter2Measurements.Add(measurement);
            });

            _meterListener!.Start();

            counter1.Add(10);
            counter2.Add(20);

            Thread.Sleep(100);

            Assert.AreEqual(1, meter1Measurements.Count);
            Assert.AreEqual(10, meter1Measurements[0]);
            Assert.AreEqual(1, meter2Measurements.Count);
            Assert.AreEqual(20, meter2Measurements[0]);
        }
    }
}
