// Copyright (C) 2015-2025 The Neo Project.
//
// TestUtility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.RestServer.Tests
{
    public static class TestUtility
    {
        public static IConfiguration CreateConfigurationFromJson(string json)
        {
            return new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes($"{{ \"PluginConfiguration\": {json} }}")))
                .Build();
        }

        public static void ConfigureRateLimiter(IServiceCollection services, RestServerSettings settings)
        {
            if (!settings.EnableRateLimiting)
                return;

            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = settings.RateLimitPermitLimit,
                            QueueLimit = settings.RateLimitQueueLimit,
                            Window = TimeSpan.FromSeconds(settings.RateLimitWindowSeconds),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }));

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.Headers.RetryAfter = settings.RateLimitWindowSeconds.ToString();

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        await context.HttpContext.Response.WriteAsync($"Too many requests. Please try again after {retryAfter.TotalSeconds} seconds.", token);
                    }
                    else
                    {
                        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
                    }
                };

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
        }
    }
}
