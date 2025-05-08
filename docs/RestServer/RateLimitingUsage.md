# Using Rate Limiting in Controllers

## Overview

This document explains how to use rate limiting at the controller and endpoint level in the Neo REST Server. Rate limiting prevents abuse by limiting the number of requests a client can make to your API within a specified time window.

## Prerequisites

Before using controller-level rate limiting, ensure:

1. Rate limiting is enabled in `RestServer.json`:
   ```json
   {
     "EnableRateLimiting": true,
     "RateLimitPermitLimit": 10,
     "RateLimitWindowSeconds": 60,
     "RateLimitQueueLimit": 0
   }
   ```

2. The necessary imports are added to your controller:
   ```csharp
   using Microsoft.AspNetCore.RateLimiting;
   ```

## Global Rate Limiting vs. Controller-level Rate Limiting

The REST Server supports two levels of rate limiting:

1. **Global Rate Limiting**: Applies to all endpoints by default when enabled in the configuration.
2. **Controller/Endpoint Rate Limiting**: Apply specific rate limiting policies to controllers or endpoints.

## Rate Limiting Attributes

### EnableRateLimiting

Apply rate limiting to a controller or specific endpoint:

```csharp
[EnableRateLimiting("policyName")]
```

### DisableRateLimiting

Disable rate limiting for a controller or specific endpoint:

```csharp
[DisableRateLimiting]
```

## Example Controller

```csharp
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("strict")]  // Apply strict rate limiting to the entire controller
public class ExampleController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("This endpoint uses the strict rate limiting policy");
    }

    [HttpGet("unlimited")]
    [DisableRateLimiting]  // Disable rate limiting for this specific endpoint
    public IActionResult GetUnlimited()
    {
        return Ok("This endpoint has no rate limiting");
    }

    [HttpGet("custom")]
    [EnableRateLimiting("custom")]  // Apply a different policy to this endpoint
    public IActionResult GetCustom()
    {
        return Ok("This endpoint uses a custom rate limiting policy");
    }
}
```

## Rate Limiting Behavior

When rate limiting is applied to a controller or endpoint, the following behaviors occur:

1. When the rate limit is reached, clients receive a `429 Too Many Requests` response.
2. The response includes a `Retry-After` header indicating when to retry.
3. The response body contains an error message explaining the rate limit.

## Priority of Rate Limiting Policies

Rate limiting policies are applied in the following order of precedence:

1. Endpoint-specific attributes (`[EnableRateLimiting]` or `[DisableRateLimiting]`)
2. Controller-level attributes
3. Global rate limiting configuration

## Important Notes

- `[DisableRateLimiting]` will disable rate limiting for a controller or endpoint regardless of parent policies.
- When applying `[EnableRateLimiting]` with a named policy, ensure the policy is defined in the rate limiter configuration.
- Controller-level rate limiting requires additional code in the `RestWebServer.cs` file. 