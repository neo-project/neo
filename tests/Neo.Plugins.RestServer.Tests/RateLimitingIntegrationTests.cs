// Copyright (C) 2015-2025 The Neo Project.
//
// RateLimitingIntegrationTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.TestHost;

namespace Neo.Plugins.RestServer.Tests
{
    [TestClass]
    public class RateLimitingIntegrationTests
    {
        private TestServer? _server;
        private HttpClient? _client;

        [TestMethod]
        public async Task RateLimiter_ShouldReturn429_WhenLimitExceeded()
        {
            // Arrange
            SetupTestServer(2, 10, 0); // 2 requests per 10 seconds, no queue

            // Act & Assert
            // First two requests should succeed
            var response1 = await _client!.GetAsync("/api/test");
            Assert.AreEqual(HttpStatusCode.OK, response1.StatusCode);

            var response2 = await _client!.GetAsync("/api/test");
            Assert.AreEqual(HttpStatusCode.OK, response2.StatusCode);

            // Third request should be rate limited
            var response3 = await _client!.GetAsync("/api/test");
            Assert.AreEqual(HttpStatusCode.TooManyRequests, response3.StatusCode);

            // Check for Retry-After header
            Assert.IsTrue(response3.Headers.Contains("Retry-After"));
            var retryAfter = response3.Headers.GetValues("Retry-After").FirstOrDefault();
            Assert.IsNotNull(retryAfter);

            // Read the response content
            var content = await response3.Content.ReadAsStringAsync();
            Assert.IsTrue(content.Contains("Too many requests"));
        }

        [TestMethod]
        public async Task RateLimiter_ShouldQueueRequests_WhenQueueLimitIsSet()
        {
            // Arrange
            SetupTestServer(2, 10, 1); // 2 requests per 10 seconds, queue 1 request

            // Act & Assert
            // First two requests should succeed immediately
            var response1 = await _client!.GetAsync("/api/test");
            Assert.AreEqual(HttpStatusCode.OK, response1.StatusCode);

            var response2 = await _client!.GetAsync("/api/test");
            Assert.AreEqual(HttpStatusCode.OK, response2.StatusCode);

            // Third request should be queued and eventually succeed
            var task3 = _client!.GetAsync("/api/test");

            // Small delay to ensure the task3 request is fully queued
            await Task.Delay(100);

            // Fourth request should be rejected (queue full)
            var response4 = await _client!.GetAsync("/api/test");
            Assert.AreEqual(HttpStatusCode.TooManyRequests, response4.StatusCode);

            // Wait for the queued request to complete
            var response3 = await task3;
            Assert.AreEqual(HttpStatusCode.OK, response3.StatusCode);
        }

        [TestMethod]
        public async Task RateLimiter_ShouldNotLimit_WhenDisabled()
        {
            // Arrange
            SetupTestServer(2, 10, 0, false); // Disabled rate limiting

            // Act & Assert
            // Multiple requests should all succeed
            for (int i = 0; i < 5; i++)
            {
                var response = await _client!.GetAsync("/api/test");
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private void SetupTestServer(int permitLimit, int windowSeconds, int queueLimit, bool enableRateLimiting = true)
        {
            // Create a test server with rate limiting
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    if (enableRateLimiting)
                    {
                        services.AddRateLimiter(options =>
                        {
                            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                                RateLimitPartition.GetFixedWindowLimiter(
                                    partitionKey: "test-client",
                                    factory: partition => new FixedWindowRateLimiterOptions
                                    {
                                        AutoReplenishment = true,
                                        PermitLimit = permitLimit,
                                        QueueLimit = queueLimit,
                                        Window = TimeSpan.FromSeconds(windowSeconds)
                                    }));

                            options.OnRejected = async (context, token) =>
                            {
                                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                                context.HttpContext.Response.Headers["Retry-After"] = windowSeconds.ToString();

                                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                                {
                                    await context.HttpContext.Response.WriteAsync($"Too many requests. Please try again after {retryAfter.TotalSeconds} seconds.", token);
                                }
                                else
                                {
                                    await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
                                }
                            };
                        });
                    }
                })
                .Configure(app =>
                {
                    if (enableRateLimiting)
                    {
                        app.UseRateLimiter();
                    }

                    app.Run(async context =>
                    {
                        if (context.Request.Path == "/api/test")
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("OK");
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                        }
                    });
                });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
            _server?.Dispose();
        }
    }
}
