// Copyright (C) 2015-2025 The Neo Project.
//
// RateLimitingTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.RestServer.Tests
{
    [TestClass]
    public class RateLimitingTests
    {
        [TestMethod]
        public void RateLimitingSettings_ShouldLoad_FromConfiguration()
        {
            // Arrange
            var settingsJson = @"{
                ""EnableRateLimiting"": true,
                ""RateLimitPermitLimit"": 5,
                ""RateLimitWindowSeconds"": 30,
                ""RateLimitQueueLimit"": 2
            }";

            var configuration = TestUtility.CreateConfigurationFromJson(settingsJson);

            // Act
            RestServerSettings.Load(configuration.GetSection("PluginConfiguration"));
            var settings = RestServerSettings.Current;

            // Assert
            Assert.IsTrue(settings.EnableRateLimiting);
            Assert.AreEqual(5, settings.RateLimitPermitLimit);
            Assert.AreEqual(30, settings.RateLimitWindowSeconds);
            Assert.AreEqual(2, settings.RateLimitQueueLimit);
        }

        [TestMethod]
        public void RateLimiter_ShouldBeConfigured_WhenEnabled()
        {
            // Arrange
            var services = new ServiceCollection();
            var settings = new RestServerSettings
            {
                EnableRateLimiting = true,
                RateLimitPermitLimit = 10,
                RateLimitWindowSeconds = 60,
                RateLimitQueueLimit = 0,
                JsonSerializerSettings = RestServerSettings.Default.JsonSerializerSettings
            };

            // Act
            var options = new RateLimiterOptions
            {
                GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = settings.RateLimitPermitLimit,
                        QueueLimit = settings.RateLimitQueueLimit,
                        Window = TimeSpan.FromSeconds(settings.RateLimitWindowSeconds)
                    }))
            };

            // Assert
            Assert.IsNotNull(options.GlobalLimiter);
        }

        [TestMethod]
        public async Task Requests_ShouldBeLimited_WhenExceedingLimit()
        {
            // Arrange
            var services = new ServiceCollection();
            var settings = new RestServerSettings
            {
                EnableRateLimiting = true,
                RateLimitPermitLimit = 2, // Set a low limit for testing
                RateLimitWindowSeconds = 10,
                RateLimitQueueLimit = 0,
                JsonSerializerSettings = RestServerSettings.Default.JsonSerializerSettings
            };

            TestUtility.ConfigureRateLimiter(services, settings);
            var serviceProvider = services.BuildServiceProvider();

            var limiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: "test-client",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = settings.RateLimitPermitLimit,
                        QueueLimit = settings.RateLimitQueueLimit,
                        Window = TimeSpan.FromSeconds(settings.RateLimitWindowSeconds)
                    }));

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

            // Act & Assert

            // First request should succeed
            var lease1 = await limiter.AcquireAsync(httpContext);
            Assert.IsTrue(lease1.IsAcquired);

            // Second request should succeed
            var lease2 = await limiter.AcquireAsync(httpContext);
            Assert.IsTrue(lease2.IsAcquired);

            // Third request should be rejected
            var lease3 = await limiter.AcquireAsync(httpContext);
            Assert.IsFalse(lease3.IsAcquired);

            // Check retry-after metadata
            Assert.IsTrue(lease3.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter));
            Assert.IsTrue(retryAfter > TimeSpan.Zero);
        }

        [TestMethod]
        public async Task RateLimiter_ShouldAllowQueuedRequests_WhenQueueLimitIsSet()
        {
            // Arrange
            var services = new ServiceCollection();
            var settings = new RestServerSettings
            {
                EnableRateLimiting = true,
                RateLimitPermitLimit = 2, // Set a low limit for testing
                RateLimitWindowSeconds = 10,
                RateLimitQueueLimit = 1, // Allow 1 queued request
                JsonSerializerSettings = RestServerSettings.Default.JsonSerializerSettings
            };

            TestUtility.ConfigureRateLimiter(services, settings);
            var serviceProvider = services.BuildServiceProvider();

            var limiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: "test-client",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = settings.RateLimitPermitLimit,
                        QueueLimit = settings.RateLimitQueueLimit,
                        Window = TimeSpan.FromSeconds(settings.RateLimitWindowSeconds)
                    }));

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

            // Act & Assert

            // First two requests should succeed immediately
            var lease1 = await limiter.AcquireAsync(httpContext);
            Assert.IsTrue(lease1.IsAcquired);

            var lease2 = await limiter.AcquireAsync(httpContext);
            Assert.IsTrue(lease2.IsAcquired);

            // Third request should be queued
            var lease3Task = limiter.AcquireAsync(httpContext);
            Assert.IsFalse(lease3Task.IsCompleted); // Should not complete immediately

            // Fourth request should be rejected (queue full)
            var lease4 = await limiter.AcquireAsync(httpContext);
            Assert.IsFalse(lease4.IsAcquired);

            // Release previous leases
            lease1.Dispose();
            lease2.Dispose();

            // The queued request should be granted
            var lease3 = await lease3Task;
            Assert.IsTrue(lease3.IsAcquired);
        }
    }
}
