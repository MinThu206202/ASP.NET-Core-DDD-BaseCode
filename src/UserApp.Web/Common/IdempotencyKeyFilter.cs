using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UserApp.Application.Common;
using UserApp.Application.Common.Interfaces;

namespace UserApp.Web.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class IdempotencyKeyFilter : Attribute, IAsyncActionFilter
{
    private static readonly HashSet<string> WriteMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE"
    };

    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(60);

    private readonly IIdempotencyService _idempotency;
    private readonly ILogger<IdempotencyKeyFilter> _logger;

    public IdempotencyKeyFilter(IIdempotencyService idempotency, ILogger<IdempotencyKeyFilter> logger)
    {
        _idempotency = idempotency;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        // Only apply to write operations
        if (!WriteMethods.Contains(request.Method))
        {
            await next();
            return;
        }

        // Require Idempotency-Key header
        if (!request.Headers.TryGetValue("Idempotency-Key", out var rawKey) ||
            string.IsNullOrWhiteSpace(rawKey.FirstOrDefault()))
        {
            SetApiJsonResult(context, 400, "Missing Idempotency-Key header for write operation.");
            return;
        }

        var headerKey = rawKey.First()!;
        var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var cacheKey = $"{headerKey}:{userId}:{request.Path}";

        try
        {
            // Check for cached response
            var cached = await _idempotency.GetCachedResponseAsync(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Idempotent replay for key {Key}", headerKey);
                context.Result = new ContentResult
                {
                    StatusCode = cached.StatusCode,
                    Content = cached.Body,
                    ContentType = cached.ContentType
                };
                return;
            }

            // Try to acquire lock (prevent concurrent duplicate processing)
            if (!await _idempotency.TryAcquireLockAsync(cacheKey, LockTimeout))
            {
                SetApiJsonResult(context, 409, "A request with this Idempotency-Key is already being processed.");
                return;
            }

            // Execute the action
            var resultContext = await next();

            // Release lock
            await _idempotency.ReleaseLockAsync(cacheKey);

            // Skip caching for error responses
            if (resultContext.Result is ObjectResult objectResult &&
                objectResult.StatusCode is >= 200 and < 300)
            {
                var body = SerializeResult(objectResult.Value);
                var statusCode = objectResult.StatusCode.Value;

                await _idempotency.StoreResponseAsync(cacheKey, new IdempotencyResult
                {
                    StatusCode = statusCode,
                    ContentType = "application/json",
                    Body = body
                }, CacheTtl);

                // Add idempotency headers to response
                context.HttpContext.Response.Headers["Idempotency-Replayed"] = "false";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Idempotency filter error for key {Key}", headerKey);
            await _idempotency.ReleaseLockAsync(cacheKey);
            throw;
        }
    }

    private static string SerializeResult(object? value)
    {
        if (value == null) return "{}";
        return System.Text.Json.JsonSerializer.Serialize(value, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        });
    }

    private static void SetApiJsonResult(ActionExecutingContext context, int statusCode, string message)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new ApiResponse<object>
        {
            Success = false,
            Message = message
        });

        context.Result = new ContentResult
        {
            StatusCode = statusCode,
            Content = json,
            ContentType = "application/json"
        };
    }
}
