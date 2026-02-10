using MAA.Application.DTOs;
using MAA.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace MAA.API.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Catches all unhandled exceptions, logs them, and returns standardized error responses.
/// Ensures no PII is exposed in error messages per security specification.
/// Feature: 006-state-context-init - User Story 3: Error Handling
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

        ErrorResponse? response = null;

        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ErrorResponse
                {
                    Error = "ValidationError",
                    Message = validationEx.Message,
                    Details = validationEx.Errors
                        .SelectMany(x => x.Value.Select(err => new ErrorDetail
                        {
                            Field = x.Key,
                            Message = err
                        }))
                        .ToList()
                };
                break;

            case EncryptionException encryptionEx:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new ErrorResponse
                {
                    Error = "EncryptionError",
                    Message = "A security operation failed. Please try again later."
                };
                // Don't expose encryption details in response
                break;

            case TimeoutException timeoutEx:
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response = new ErrorResponse
                {
                    Error = "Timeout",
                    Message = "The request timed out. Please try again."
                };
                break;

            case ArgumentNullException argNullEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ErrorResponse
                {
                    Error = "ValidationError",
                    Message = "One or more required parameters are missing."
                };
                break;

            case InvalidOperationException invalidOpEx:
                // Check if it's a concurrency conflict
                if (invalidOpEx.Message.Contains("modified by another process"))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response = new ErrorResponse
                    {
                        Error = "Conflict",
                        Message = "The resource was modified by another user. Please refresh and try again."
                    };
                }
                else if (invalidOpEx.Message.Contains("not found"))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response = new ErrorResponse
                    {
                        Error = "NotFound",
                        Message = "The requested resource was not found."
                    };
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new ErrorResponse
                    {
                        Error = "InvalidOperation",
                        Message = "The request cannot be processed due to a business rule violation."
                    };
                }
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = new ErrorResponse
                {
                    Error = "Unauthorized",
                    Message = "Authentication is required to access this resource."
                };
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = new ErrorResponse
                {
                    Error = "NotFound",
                    Message = "The requested resource was not found."
                };
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new ErrorResponse
                {
                    Error = "InternalServerError",
                    Message = "An unexpected error occurred. Please contact support if the problem persists."
                };
                break;
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsJsonAsync(response!, options);
    }
}
