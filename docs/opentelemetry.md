// Copyright (C) 2015-2025 The Neo Project.
//
// opentelemetry.md belongs to the neo project and is free software distributed under the MIT software
// license, see the accompanying file LICENSE in the main directory of the repository or
// http://www.opensource.org/licenses/mit-license.php for more details.

# Neo Observability & Telemetry

This document explains how Neo’s OpenTelemetry plugin turns a validator or full node into a fully
instrumented service, suitable for production monitoring, profiling, and capacity planning. It covers:

- Installing and enabling the plugin
- Configuring exporters, resource attributes, and per-area metric toggles
- Understanding the metric families and dashboards that ship with the repository
- Leveraging consensus/state/RPC diagnostics, VM EventCounters, and hot-trace profiling
- Operating guidance for Prometheus, Grafana, and on-call alerting

The plugin targets .NET 9 and uses OpenTelemetry 1.12.0. Metrics are hosted from within the node
process and do **not** require ASP.NET Core.

---

## Installation

1. Copy `src/Plugins/OTelPlugin` into your node’s `Plugins` directory.
2. Ensure the plugin DLL (`OTelPlugin.dll`) is placed under `Plugins/OpenTelemetry`.
3. Start the node; on success you will see `OpenTelemetry plugin initialized successfully`.

The plugin loads before DBFT and StateService so that consensus/state diagnostics are emitted from
the first event.

---

## Configuration (`Plugins/OpenTelemetry/OTelPlugin.json`)

```json
{
  "PluginConfiguration": {
    "Enabled": true,
    "ServiceName": "neo-node",
    "InstanceId": "validator-01",
    "UnhandledExceptionPolicy": "StopPlugin",
    "Metrics": {
      "Enabled": true,
      "Interval": 10000,
      "Categories": {
        "Blockchain": true,
        "Mempool": true,
        "Network": true,
        "System": true,
        "Consensus": true,
        "State": true,
        "Vm": true,
        "Rpc": true
      },
      "PrometheusExporter": {
        "Enabled": true,
        "Port": 9090,
        "Path": "/metrics"
      },
      "ConsoleExporter": {
        "Enabled": false
      }
    },
    "Traces": {
      "Enabled": false,
      "ConsoleExporter": {
        "Enabled": false
      }
    },
    "Logs": {
      "Enabled": false,
      "ConsoleExporter": {
        "Enabled": false
      }
    },
    "OtlpExporter": {
      "Enabled": false,
      "Endpoint": "http://localhost:4317",
      "Protocol": "grpc",
      "Timeout": 10000,
      "Headers": "",
      "ExportMetrics": true,
      "ExportTraces": false,
      "ExportLogs": false
    },
    "ResourceAttributes": {
      "deployment.environment": "production",
      "service.namespace": "neo",
      "host.name": "validator-01"
    }
  }
}
```

### Metrics Categories

Each category gates an entire block of instruments and associated collectors:

| Category   | Contents                                                                          |
|------------|-----------------------------------------------------------------------------------|
| Blockchain | Block/tx counters, processing latency histogram, block rate gauge                 |
| Mempool    | Verified/unverified counts, capacity ratio, eviction histograms                   |
| Network    | Connected/unconnected peers                                                       |
| System     | CPU, GC, thread count, disk usage, file descriptors, readiness/health            |
| Consensus  | View changes, proposal latency, message counters, round/view gauges               |
| State      | State root heights, lag, snapshot timings, validation counters                    |
| Vm         | EventCounters, stack depths, trace profile metrics, super-instruction planner     |
| Rpc        | In-flight requests, throughput, latency histogram, error counts with tags         |

Set a category to `false` to suppress initialization and background collectors for that area.
`telemetry status` reflects the current toggle state.

### Exporters

- **Prometheus HTTP listener** *(default)*: exposes `/metrics` on the configured port/path.
- **Console exporter**: useful when debugging locally.
- **OTLP gRPC/HTTP**: sends metrics to an OpenTelemetry Collector or SaaS endpoint.

Due to the daemon-style runtime, the plugin validates ports, OTLP endpoints, and sanitises headers
before starting exporters. Any misconfiguration is surfaced during bootstrap.

### Resource Attributes

Additional key/value attributes enrich metrics with deployment metadata. Common keys include
`deployment.environment`, `service.namespace`, `neo.network`, `cloud.region`, and `cluster`.

---

## Diagnostics & Metric Families

### Blockchain & Mempool
- `neo.blocks.processed_total`, `neo.block.processing_time`
- `neo.mempool.size`, `neo.mempool.conflicts_total`, `neo.mempool.capacity_ratio`

Event points:
- `Blockchain_Committing_Handler` counts transactions, contract invocations, and verification failures.
- `Blockchain_Committed_Handler` records block latency and updates history for the rate gauge.

### Consensus (dBFT)
- Gauges for round, view, primary index (`neo.consensus.round`, `neo.consensus.view`, `neo.consensus.state`)
- View change counter tagged by reason (`neo.consensus.view_changes_total`)
- Message counters for sent/received payloads with type tags
- Finality latency gauge (`neo.consensus.time_to_finality`)

Hooked via `IConsensusDiagnosticsHandler` to capture start, view change, message, and commit events.

### State Service
- Local vs validated state root heights, lag, snapshot apply/commit durations
- Validation and error counters with stage/reason labels

### RPC
- Active in-flight requests, per-method request totals and errors, latency histogram (ms)
- `IRpcDiagnosticsHandler` records completion status, error codes, and method names.

### System Health
- Process/system CPU, memory, threads, file descriptors, disk free space, chain DB size
- Readiness (`neo.node.readiness`) reports 0 while the node is more than 10 blocks behind.
- Health score summarises dashboard liveness checks and metric errors.

### VM Observability
- EventCounter integration for instruction dispatch rate/latency, stack depth, ref sweep rate
- Trace profiler persisting hot opcode windows to `Plugins/OpenTelemetry/profiles`
- Super-instruction planner gauge showing current plan count
- Grafana dashboards include profiling guidance and links to generated C# stubs via `tools/generate_superinstructions.py`

---

## Monitoring Stack

The repository ships with turnkey assets under `src/Plugins/OTelPlugin/monitoring`:

- **Prometheus configuration & alert rules** (`prometheus-alerts.yml`)
  - Critical: Node down, blockchain stuck, peerless, storage corruption
  - Warning: High CPU/memory, mempool saturation, consensus view flapping, RPC backlog
- **Grafana dashboards** (`neo-dashboard.json` & provisioning bundle)
  - Node/system, consensus, state service, RPC, VM performance, mempool
  - Panels automatically hide if the underlying metrics are disabled
- **Metrics simulator** (`metrics-simulator.py`) for local dashboards without a running node
- **Docker Compose** scripts (`run-local.sh`) to start Prometheus + Grafana quickly

### Deploying Grafana / Prometheus

1. `docker-compose up -d` inside `monitoring/` for local evaluation.
2. Import `neo-dashboard.json` or use the provisioning bundle.
3. Configure Alertmanager to route alerts (email, Slack, PagerDuty, …).
4. For production, run Prometheus in HA pairs with remote storage/federation as appropriate.

---

## Advanced Observability & Tuning

### EventCounters

The plugin registers `VmEventCounterListener` to subscribe to .NET EventCounters emitted by the VM:
- Dispatch rate, instruction latency
- Evaluation/invocation/result stack depths
- Reference sweep rates

Ensure `DOTNET_EnableDiagnostics=1` in service environments where the default is disabled.

### Trace Profiling & Super-instruction Planning

- `VmTraceProfiler` records frequently executed opcode sequences per contract hash.
- `VmTraceProfileStore` writes JSON profiles under `Plugins/OpenTelemetry/profiles`.
- `VmSuperInstructionPlanner` converts traces into optimisation plans surfaced to the CLI/dashboards.
- Use `tools/generate_superinstructions.py` to transform profiles into partial C# helpers for the VM.

### Hotspot Analysis Workflow

1. Enable VM category and allow the profiler to capture workload traces.
2. Inspect Grafana VM panels for high hot-trace ratios.
3. Export planner suggestions via `telemetry plans` or the generated JSON.
4. Feed the sequences into the VM’s super-instruction table or JIT pipeline.

---

## Operational Playbook

- **Startup validation**: Verify `telemetry status` shows expected categories and exporters.
- **Connectivity**: Prometheus target `http://node:9090/metrics` should scrape without TLS errors.
- **Alert hygiene**: Tune thresholds to your network; defaults align with mainnet validators.
- **Capacity management**: Monitor `neo.node.disk_free_bytes` and `neo.node.chain_db_size_bytes`
  together to anticipate storage expansion.
- **Consensus health**: View change spikes, finality latency, or message imbalances often indicate
  network issues or misconfigured peers.
- **RPC SLOs**: Track latency histogram percentiles in Grafana; use alert thresholds for p95 > 500 ms.
- **State service**: `neo.state.snapshot_health` dipping below 0.9 signals lagging validation partners.

---

## Testing

- Run `dotnet test neo.sln` to exercise the entire suite, including telemetry unit tests.
- `tests/Neo.Plugins.OTelPlugin.Tests` contains reflection-based cases verifying category toggles.
- Integration tests for specific exporters can be added by mocking telemetry handlers or scraping
  the Prometheus endpoint in CI.

---

## Troubleshooting

| Symptom | Diagnosis | Remedy |
|---------|-----------|--------|
| No `/metrics` output | Prometheus exporter disabled, port conflict, or plugin disabled | Enable exporter, verify port availability, check logs |
| Missing consensus metrics | `Metrics.Categories.Consensus=false` or DBFT plugin loaded without diagnostics handlers | Enable category, ensure OpenTelemetry loads before DBFT |
| Grafana panels blank | Incorrect Prometheus datasource, category disabled | Update datasource, adjust dashboards or enable category |
| High CPU from profiler | VM category enabled on low-power hardware | Disable `Vm` category or reduce metric interval |
| Chain DB size `0` | Node running without persisted storage | Start the node with a persistent chain directory |

Logs include detailed error messages whenever exporters fail to start or diagnostics handlers throw.

---

## References

- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
- [Prometheus Operator documentation](https://prometheus-operator.dev/)
- [Grafana Alerting](https://grafana.com/docs/grafana/latest/alerting/)

For questions or issues file a ticket at [github.com/neo-project/neo](https://github.com/neo-project/neo).
