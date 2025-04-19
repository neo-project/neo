# Rate Limiting in Neo REST Server

## Overview

The rate limiting feature in the Neo REST Server plugin provides protection against abuse by limiting the number of requests a client can make in a given time period. This helps maintain stability, security, and performance of the REST API.

## Configuration

Rate limiting can be configured in the `RestServer.json` file with the following options:

| Parameter | Type | Description |
|-----------|------|-------------|
| `EnableRateLimiting` | boolean | Enables or disables rate limiting |
| `RateLimitPermitLimit` | integer | Maximum number of requests allowed in the specified time window |
| `RateLimitWindowSeconds` | integer | The time window in seconds for rate limiting |
| `RateLimitQueueLimit` | integer | Number of requests to queue when limit is exceeded (0 to disable queuing) |

## Default Configuration

```json
{
  "EnableRateLimiting": true,
  "RateLimitPermitLimit": 10,
  "RateLimitWindowSeconds": 60,
  "RateLimitQueueLimit": 0
}
```

By default, the configuration allows 10 requests per minute per IP address.

## How It Works

The REST Server uses ASP.NET Core's built-in rate limiting middleware (`Microsoft.AspNetCore.RateLimiting`) to implement a fixed window rate limiter. This means:

1. Requests are tracked based on the client's IP address
2. A fixed time window (configured by `RateLimitWindowSeconds`) determines the period for counting requests
3. When the limit is reached, clients receive a 429 (Too Many Requests) response with a Retry-After header

## Response Format

When a client exceeds the rate limit, they receive:

- HTTP Status: 429 Too Many Requests
- Header: `Retry-After: [seconds]`
- Body: Error message indicating when they can try again

## Use Cases

Rate limiting is particularly useful for:

1. **Preventing API Abuse**: Limits the number of requests a user or client can make
2. **Ensuring Fair Usage**: Prevents individual clients from monopolizing server resources
3. **Protecting Resources**: Controls the number of requests to prevent server overload
4. **Enhancing Security**: Helps mitigate certain types of denial of service attacks

## Important Notes

- Rate limiting is applied at the IP address level and affects all endpoints
- If your application has legitimate high-volume needs, consider adjusting the limits accordingly
- For applications with multiple clients behind a single IP (e.g., corporate proxies), consider implementing your own rate limiting logic that takes into account application-specific identifiers 