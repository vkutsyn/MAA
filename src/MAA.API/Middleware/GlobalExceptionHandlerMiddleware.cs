using MAA.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace MAA.API.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Catches all unhandled exceptions, logs them, and returns standardized error responses.
/// Ensures no PII is exposed in error messages per security specification.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of GlobalExceptionHandlerMiddleware.
    /// </summary>
    /// <param name="next">Next delegate in pipeline</param>
    /// <param name="logger">Logger instance</param>
    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request.
    /// </summary>
    /// <param name="context">HTTP context</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
            await HandleExceptionAsync(context, exception);
        }
    }

    /// <summary>
    /// Handles exceptions and writes standardized error response.
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="exception">Exception to handle</param>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusCode = 400;
                response.Message = "Validation failed. Please check the provided data.";
                response.Errors = validationEx.Errors.ToDictionary(
                    x => x.Key,
                    x => (IList<string>)x.Value.ToList());
                break;

            case EncryptionException encryptionEx:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.StatusCode = 500;
                response.Message = "A security operation failed. Please try again later.";
                response.TraceId = context.TraceIdentifier;
                // Don't expose encryption details in response
                break;

            case TimeoutException timeoutEx:
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.StatusCode = 408;
                response.Message = "The request timed out. Please try again.";
                response.TraceId = context.TraceIdentifier;
                break;

            case ArgumentNullException argNullEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusCode = 400;
                response.Message = "One or more required parameters are missing.";
                break;

            case InvalidOperationException invalidOpEx:
                // Check if it's a concurrency conflict
                if (invalidOpEx.Message.Contains("modified by another process"))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.StatusCode = 409;
                    response.Message = "The resource was modified by another user. Please refresh and try again.";
                }
                else if (invalidOpEx.Message.Contains("not found"))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.StatusCode = 404;
                    response.Message = "The requested resource was not found.";
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.StatusCode = 400;
                    response.Message = "The request cannot be processed due to a business rule violation.";
                }
                response.TraceId = context.TraceIdentifier;
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.StatusCode = 401;
                response.Message = "Authentication is required to access this resource.";
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusCode = 404;
                response.Message = "The requested resource was not found.";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.StatusCode = 500;
                response.Message = "An unexpected error occurred. Please contact support if the problem persists.";
                response.TraceId = context.TraceIdentifier;
                break;
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsJsonAsync(response, options);
    }

    /// <summary>
    /// Standard error response model sent to clients.
    /// </summary>
    private class ErrorResponse
    {
        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// User-friendly error message (no PII).
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Structured validation errors (if applicable).
        /// </summary>
        public IDictionary<string, IList<string>>? Errors { get; set; }

        /// <summary>
        /// Request trace ID for support inquiry correlation.
        /// </summary>
        public string? TraceId { get; set; }
    }
}
