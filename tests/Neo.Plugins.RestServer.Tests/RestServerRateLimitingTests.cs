// Copyright (C) 2015-2025 The Neo Project.
//
// RestServerRateLimitingTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;

namespace Neo.Plugins.RestServer.Tests
{
    [TestClass]
    public class RestServerRateLimitingTests
    {
        private TestServer? _server;
        private HttpClient? _client;

        [TestInitialize]
        public void Initialize()
        {
            // Create a configuration with rate limiting enabled
            var configJson = @"{
                ""Network"": 860833102,
                ""BindAddress"": ""127.0.0.1"",
                ""Port"": 10339,
                ""EnableRateLimiting"": true,
                ""RateLimitPermitLimit"": 2,
                ""RateLimitWindowSeconds"": 10,
                ""RateLimitQueueLimit"": 0
            }";

            var configuration = new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes($"{{ \"PluginConfiguration\": {configJson} }}")))
                .Build();

            // Load the settings
            RestServerSettings.Load(configuration.GetSection("PluginConfiguration"));

            // Create a test server with a simple endpoint
            var host = new HostBuilder().ConfigureWebHost(builder =>
            {
                builder.UseTestServer().ConfigureServices(services =>
                {
                    // Add services to build the RestWebServer
                    services.AddRouting();
                    ConfigureRestServerServices(services, RestServerSettings.Current);
                }).Configure(app =>
                {
                    // Configure the middleware pipeline similar to RestWebServer
                    if (RestServerSettings.Current.EnableRateLimiting)
                    {
                        app.UseRateLimiter();
                    }

                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/test", async context =>
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("OK");
                        });
                    });
                });
            }).Build();

            host.Start();
            _server = host.GetTestServer();
            _client = _server.CreateClient();
        }

        [TestMethod]
        public async Task RestServer_ShouldRateLimit_WhenLimitExceeded()
        {
            // Act & Assert
            // First two requests should succeed
            var response1 = await _client!.GetAsync("/api/test", CancellationToken.None);
            Assert.AreEqual(HttpStatusCode.OK, response1.StatusCode);

            var response2 = await _client!.GetAsync("/api/test", CancellationToken.None);
            Assert.AreEqual(HttpStatusCode.OK, response2.StatusCode);

            // Third request should be rate limited
            var response3 = await _client!.GetAsync("/api/test", CancellationToken.None);
            Assert.AreEqual(HttpStatusCode.TooManyRequests, response3.StatusCode);

            // Check for Retry-After header
            Assert.Contains((header) => header.Key == "Retry-After", response3.Headers);

            // Read the response content
            var content = await response3.Content.ReadAsStringAsync(CancellationToken.None);
            Assert.Contains("Too many requests", content);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
            _server?.Dispose();
        }

        // Helper method to configure services similar to RestWebServer
        private void ConfigureRestServerServices(IServiceCollection services, RestServerSettings settings)
        {
            // Extract rate limiting configuration code from RestWebServer using reflection
            // This is a test-only approach to get the actual configuration logic
            try
            {
                // Here we use the TestUtility helper
                TestUtility.ConfigureRateLimiter(services, settings);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to configure services: {ex}");
            }
        }
    }
}
