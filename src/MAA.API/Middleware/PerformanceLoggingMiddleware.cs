using System.Diagnostics;

namespace MAA.API.Middleware;

/// <summary>
/// Middleware for logging performance timing of evaluation requests.
/// Tracks p95 and p99 latencies to monitor SLA compliance.
/// </summary>
public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;

    public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only track specific performance-sensitive endpoints
        if (!IsPerformanceSensitiveEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            LogPerformance(context, stopwatch.ElapsedMilliseconds);
        }
    }

    private bool IsPerformanceSensitiveEndpoint(PathString path)
    {
        // Track eligibility evaluation endpoint
        return path.StartsWithSegments("/api/eligibility/evaluate", StringComparison.OrdinalIgnoreCase);
    }

    private void LogPerformance(HttpContext context, long elapsedMilliseconds)
    {
        var level = GetLogLevel(elapsedMilliseconds);
        
        _logger.Log(
            level,
            "Eligibility evaluation request completed in {ElapsedMs}ms. " +
            "StatusCode: {StatusCode}, Path: {Path}",
            elapsedMilliseconds,
            context.Response.StatusCode,
            context.Request.Path);

        // Log warning if approaching SLO limits
        if (elapsedMilliseconds > 1500) // p95 SLO is 2s, warn at 75%
        {
            _logger.LogWarning(
                "Eligibility evaluation approaching p95 SLO. " +
                "Elapsed: {ElapsedMs}ms (SLO: 2000ms), Path: {Path}",
                elapsedMilliseconds,
                context.Request.Path);
        }

        if (elapsedMilliseconds > 3750) // p99 SLO is 5s, warn at 75%
        {
            _logger.LogWarning(
                "Eligibility evaluation approaching p99 SLO. " +
                "Elapsed: {ElapsedMs}ms (SLO: 5000ms), Path: {Path}",
                elapsedMilliseconds,
                context.Request.Path);
        }

        if (elapsedMilliseconds > 5000)
        {
            _logger.LogError(
                "Eligibility evaluation SLO EXCEEDED. " +
                "Elapsed: {ElapsedMs}ms (p99 SLO: 5000ms), Path: {Path}",
                elapsedMilliseconds,
                context.Request.Path);
        }
    }

    private LogLevel GetLogLevel(long elapsedMilliseconds)
    {
        return elapsedMilliseconds switch
        {
            <= 500 => LogLevel.Debug,
            <= 2000 => LogLevel.Information,
            <= 5000 => LogLevel.Warning,
            _ => LogLevel.Error
        };
    }
}

/// <summary>
/// Extension methods for registering performance logging middleware.
/// </summary>
public static class PerformanceLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds performance logging middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UsePerformanceLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceLoggingMiddleware>();
    }
}
