// Copyright (C) 2015-2025 The Neo Project.
//
// HealthCheckEndpoint.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.SmartContract.Native;
using System.Net;
using System.Text;

namespace Neo.Plugins.Telemetry.Health
{
    /// <summary>
    /// Provides a simple HTTP health check endpoint for the Neo node.
    /// </summary>
    public sealed class HealthCheckEndpoint : IDisposable
    {
        private readonly NeoSystem _system;
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts;
        private readonly Task _listenerTask;
        private readonly string _nodeId;
        private readonly string _network;
        private bool _disposed;

        public HealthCheckEndpoint(NeoSystem system, string host, int port, string nodeId, string network)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _nodeId = nodeId;
            _network = network;
            _cts = new CancellationTokenSource();

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{host}:{port}/health/");
            _listener.Prefixes.Add($"http://{host}:{port}/ready/");
            _listener.Prefixes.Add($"http://{host}:{port}/live/");

            try
            {
                _listener.Start();
                _listenerTask = Task.Run(ListenAsync, _cts.Token);
                Utility.Log(nameof(HealthCheckEndpoint), LogLevel.Info,
                    $"Health check endpoint started on http://{host}:{port}/health/");
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(HealthCheckEndpoint), LogLevel.Warning,
                    $"Failed to start health check endpoint: {ex.Message}");
                _listener = null!;
                _listenerTask = Task.CompletedTask;
            }
        }

        private async Task ListenAsync()
        {
            while (!_cts.Token.IsCancellationRequested && _listener?.IsListening == true)
            {
                try
                {
                    var context = await _listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => HandleRequestAsync(context), _cts.Token);
                }
                catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Utility.Log(nameof(HealthCheckEndpoint), LogLevel.Debug,
                        $"Error handling health check request: {ex.Message}");
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            try
            {
                var path = context.Request.Url?.AbsolutePath?.ToLowerInvariant() ?? "";
                var response = path switch
                {
                    "/health/" or "/health" => GetHealthStatus(),
                    "/ready/" or "/ready" => GetReadinessStatus(),
                    "/live/" or "/live" => GetLivenessStatus(),
                    _ => GetHealthStatus()
                };

                var json = response.ToString();
                var buffer = Encoding.UTF8.GetBytes(json);

                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.StatusCode = response["status"]?.GetString() == "healthy" ? 200 : 503;

                await context.Response.OutputStream.WriteAsync(buffer, _cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(HealthCheckEndpoint), LogLevel.Debug,
                    $"Error writing health check response: {ex.Message}");
            }
            finally
            {
                context.Response.Close();
            }
        }

        private JObject GetHealthStatus()
        {
            try
            {
                var currentHeight = NativeContract.Ledger.CurrentIndex(_system.StoreView);
                var headerHeight = _system.HeaderCache.Last?.Index ?? currentHeight;
                var blocksBehind = headerHeight - currentHeight;
                var isSynced = blocksBehind <= 2;
                var peerCount = 0;

                var memPool = _system.MemPool;
                var mempoolCount = memPool.Count;
                var mempoolCapacity = memPool.Capacity;

                var status = isSynced && peerCount >= 0 ? "healthy" : "degraded";

                return new JObject
                {
                    ["status"] = status,
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["node_id"] = _nodeId,
                    ["network"] = _network,
                    ["checks"] = new JObject
                    {
                        ["blockchain"] = new JObject
                        {
                            ["status"] = isSynced ? "healthy" : "degraded",
                            ["block_height"] = currentHeight,
                            ["header_height"] = headerHeight,
                            ["blocks_behind"] = blocksBehind,
                            ["synced"] = isSynced
                        },
                        ["mempool"] = new JObject
                        {
                            ["status"] = "healthy",
                            ["count"] = mempoolCount,
                            ["capacity"] = mempoolCapacity,
                            ["utilization"] = mempoolCapacity > 0 ? (double)mempoolCount / mempoolCapacity : 0
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return new JObject
                {
                    ["status"] = "unhealthy",
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["error"] = ex.Message
                };
            }
        }

        private JObject GetReadinessStatus()
        {
            try
            {
                var currentHeight = NativeContract.Ledger.CurrentIndex(_system.StoreView);
                var headerHeight = _system.HeaderCache.Last?.Index ?? currentHeight;
                var isSynced = (headerHeight - currentHeight) <= 2;

                return new JObject
                {
                    ["status"] = isSynced ? "healthy" : "not_ready",
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["synced"] = isSynced,
                    ["block_height"] = currentHeight
                };
            }
            catch (Exception ex)
            {
                return new JObject
                {
                    ["status"] = "unhealthy",
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["error"] = ex.Message
                };
            }
        }

        private static JObject GetLivenessStatus()
        {
            return new JObject
            {
                ["status"] = "healthy",
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _cts.Cancel();
                _listener?.Stop();
                _listener?.Close();
                _listenerTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(HealthCheckEndpoint), LogLevel.Debug,
                    $"Error disposing health check endpoint: {ex.Message}");
            }
            finally
            {
                _cts.Dispose();
            }
        }
    }
}
