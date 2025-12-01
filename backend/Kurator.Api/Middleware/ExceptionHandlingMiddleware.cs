using System.Net;
using System.Text.Json;
using Serilog.Context;

namespace Kurator.Api.Middleware;

/// <summary>
/// Middleware for handling exceptions with detailed logging to stdout.
/// Provides structured error responses and comprehensive exception logging.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Response.Headers["X-Correlation-Id"].FirstOrDefault() ?? context.TraceIdentifier;
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("UserId", userId))
            {
                // Log comprehensive exception details
                _logger.LogError(ex,
                    "[{CorrelationId}] Unhandled exception in request pipeline. " +
                    "Method: {Method}, Path: {Path}, QueryString: {QueryString}, " +
                    "User: {UserId}, ClientIP: {ClientIP}, " +
                    "ExceptionType: {ExceptionType}, Message: {ExceptionMessage}",
                    correlationId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.QueryString,
                    userId,
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    ex.GetType().FullName,
                    ex.Message);

                // Log inner exceptions if present
                var innerEx = ex.InnerException;
                var depth = 1;
                while (innerEx != null)
                {
                    _logger.LogError(
                        "[{CorrelationId}] Inner Exception [{Depth}]: {ExceptionType} - {Message}",
                        correlationId, depth, innerEx.GetType().FullName, innerEx.Message);
                    innerEx = innerEx.InnerException;
                    depth++;
                }

                await HandleExceptionAsync(context, ex, correlationId);
            }
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        // Check if response has already started
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("[{CorrelationId}] Response has already started, cannot write error response",
                correlationId);
            return;
        }

        object response;
        string errorType;
        string errorMessage;

        switch (exception)
        {
            case ArgumentNullException argNullEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorType = "ValidationError";
                errorMessage = argNullEx.Message;
                _logger.LogWarning("[{CorrelationId}] Validation error (ArgumentNull): {ParameterName}",
                    correlationId, argNullEx.ParamName);
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorType = "ValidationError";
                errorMessage = argEx.Message;
                _logger.LogWarning("[{CorrelationId}] Validation error (Argument): {Message}",
                    correlationId, argEx.Message);
                break;

            case UnauthorizedAccessException _:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorType = "AuthenticationError";
                errorMessage = "Unauthorized access";
                _logger.LogWarning("[{CorrelationId}] Unauthorized access attempt",
                    correlationId);
                break;

            case KeyNotFoundException _:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                errorType = "NotFoundError";
                errorMessage = "The requested resource was not found";
                _logger.LogWarning("[{CorrelationId}] Resource not found",
                    correlationId);
                break;

            case InvalidOperationException invalidOpEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorType = "InvalidOperationError";
                errorMessage = invalidOpEx.Message;
                _logger.LogWarning("[{CorrelationId}] Invalid operation: {Message}",
                    correlationId, invalidOpEx.Message);
                break;

            case OperationCanceledException _:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorType = "OperationCancelled";
                errorMessage = "The operation was cancelled";
                _logger.LogWarning("[{CorrelationId}] Operation was cancelled",
                    correlationId);
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorType = "InternalServerError";
                errorMessage = _environment.IsDevelopment() ? exception.Message : "An internal server error occurred";
                break;
        }

        if (_environment.IsDevelopment())
        {
            response = new
            {
                error = new
                {
                    message = errorMessage,
                    type = errorType,
                    correlationId = correlationId,
                    exceptionType = exception.GetType().FullName,
                    stackTrace = exception.StackTrace,
                    innerException = exception.InnerException?.Message
                }
            };
        }
        else
        {
            response = new
            {
                error = new
                {
                    message = errorMessage,
                    type = errorType,
                    correlationId = correlationId
                }
            };
        }

        _logger.LogDebug("[{CorrelationId}] Sending error response: StatusCode={StatusCode}, ErrorType={ErrorType}",
            correlationId, context.Response.StatusCode, errorType);

        try
        {
            context.Response.ContentType = "application/json";
            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            });

            await context.Response.WriteAsync(jsonResponse);
        }
        catch (ObjectDisposedException)
        {
            // Stream was already disposed by another middleware
            _logger.LogWarning("[{CorrelationId}] Could not write error response - stream already disposed",
                correlationId);
        }
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}