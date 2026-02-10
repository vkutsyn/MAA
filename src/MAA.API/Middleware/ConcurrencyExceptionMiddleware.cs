using System.Net;
using System.Text.Json;
using MAA.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MAA.API.Middleware;

/// <summary>
/// Middleware to map EF Core concurrency exceptions to HTTP 409 responses.
/// </summary>
public class ConcurrencyExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ConcurrencyExceptionMiddleware> _logger;

    public ConcurrencyExceptionMiddleware(RequestDelegate next, ILogger<ConcurrencyExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict detected");
            await WriteConflictAsync(context);
        }
    }

    private static Task WriteConflictAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Conflict;

        var response = new ConcurrencyErrorResponse
        {
            Error = "ConcurrencyConflict",
            Message = "The resource was modified by another process. Please refresh and try again.",
            Current = null
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsJsonAsync(response, options);
    }
}
