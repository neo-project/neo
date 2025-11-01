// Copyright (C) 2015-2025 The Neo Project.
//
// VmEventCounterListener.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Neo.Plugins.OpenTelemetry
{
    internal sealed class VmEventCounterListener : EventListener, IDisposable
    {
        private const string EventSourceName = "Neo.VM.Execution";
        private readonly object _syncRoot = new();
        private readonly Dictionary<string, double> _latestValues = new(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);

            if (eventSource.Name.Equals(EventSourceName, StringComparison.Ordinal))
            {
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.None,
                    new Dictionary<string, string?>
                    {
                        ["EventCounterIntervalSec"] = "1"
                    });
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName != "EventCounters" || eventData.Payload == null || eventData.Payload.Count == 0)
                return;

            if (eventData.Payload[0] is not IDictionary<string, object?> payload)
                return;

            if (!payload.TryGetValue("Name", out var nameObj) || nameObj is not string counterName)
                return;

            double value = 0d;
            if (payload.TryGetValue("Mean", out var meanObj) && meanObj != null)
            {
                value = Convert.ToDouble(meanObj);
            }
            else if (payload.TryGetValue("Increment", out var incrementObj) && incrementObj != null)
            {
                value = Convert.ToDouble(incrementObj);
            }
            else if (payload.TryGetValue("Value", out var valueObj) && valueObj != null)
            {
                value = Convert.ToDouble(valueObj);
            }

            lock (_syncRoot)
            {
                _latestValues[counterName] = value;
            }
        }

        public double GetValue(string counterName)
        {
            lock (_syncRoot)
            {
                return _latestValues.TryGetValue(counterName, out var value) ? value : 0d;
            }
        }

        public void Shutdown()
        {
            if (_disposed) return;
            foreach (var source in EventSource.GetSources())
            {
                if (source.Name.Equals(EventSourceName, StringComparison.Ordinal))
                    DisableEvents(source);
            }
            _disposed = true;
            base.Dispose();
        }
    }
}
