using System.Diagnostics;
using System.Text;
using Serilog;
using Serilog.Context;

namespace Kurator.Api.Middleware;

/// <summary>
/// Middleware for detailed request/response logging to stdout.
/// Logs all HTTP requests and responses with headers, body, timing, and user context.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-API-Key",
        "X-Auth-Token"
    };

    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordHash",
        "secret",
        "token",
        "refreshToken",
        "mfaSecret",
        "totpCode",
        "privateKey",
        "encryptionKey"
    };

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate correlation ID for request tracing
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // Get user ID if authenticated
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        // Push properties to log context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        {
            var stopwatch = Stopwatch.StartNew();

            // Log the incoming request
            await LogRequest(context, correlationId);

            // Capture the original response body stream
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[{CorrelationId}] Request failed after {ElapsedMs}ms. Method: {Method}, Path: {Path}, Exception: {ExceptionType}, Message: {ExceptionMessage}",
                    correlationId, stopwatch.ElapsedMilliseconds, context.Request.Method, context.Request.Path,
                    ex.GetType().Name, ex.Message);
                throw;
            }

            stopwatch.Stop();

            // Log the response
            await LogResponse(context, correlationId, stopwatch.ElapsedMilliseconds, responseBody);

            // Copy the response body back to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task LogRequest(HttpContext context, string correlationId)
    {
        var request = context.Request;

        // Log basic request info
        _logger.LogDebug(
            "[{CorrelationId}] >>> Incoming Request: {Method} {Scheme}://{Host}{Path}{QueryString}",
            correlationId, request.Method, request.Scheme, request.Host, request.Path, request.QueryString);

        // Log request headers (masking sensitive ones)
        var headers = new Dictionary<string, string>();
        foreach (var header in request.Headers)
        {
            if (SensitiveHeaders.Contains(header.Key))
            {
                headers[header.Key] = "***MASKED***";
            }
            else
            {
                headers[header.Key] = string.Join(", ", header.Value.ToArray());
            }
        }
        _logger.LogDebug("[{CorrelationId}] Request Headers: {@Headers}", correlationId, headers);

        // Log request body for non-GET requests
        if (request.ContentLength > 0 && !HttpMethods.IsGet(request.Method))
        {
            request.EnableBuffering();
            var body = await ReadRequestBody(request);

            if (!string.IsNullOrEmpty(body))
            {
                var maskedBody = MaskSensitiveData(body);
                _logger.LogDebug("[{CorrelationId}] Request Body: {Body}", correlationId, maskedBody);
            }

            request.Body.Position = 0;
        }

        // Log connection info
        _logger.LogDebug(
            "[{CorrelationId}] Connection Info: ClientIP={ClientIP}, Protocol={Protocol}, ContentType={ContentType}, ContentLength={ContentLength}",
            correlationId,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            request.Protocol,
            request.ContentType ?? "none",
            request.ContentLength ?? 0);
    }

    private async Task LogResponse(HttpContext context, string correlationId, long elapsedMs, MemoryStream responseBody)
    {
        var response = context.Response;

        // Log basic response info
        var logLevel = response.StatusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Debug
        };

        _logger.Log(logLevel,
            "[{CorrelationId}] <<< Outgoing Response: {StatusCode} in {ElapsedMs}ms",
            correlationId, response.StatusCode, elapsedMs);

        // Log response headers
        var headers = new Dictionary<string, string>();
        foreach (var header in response.Headers)
        {
            if (SensitiveHeaders.Contains(header.Key))
            {
                headers[header.Key] = "***MASKED***";
            }
            else
            {
                headers[header.Key] = string.Join(", ", header.Value.ToArray());
            }
        }
        _logger.LogDebug("[{CorrelationId}] Response Headers: {@Headers}", correlationId, headers);

        // Log response body (if JSON and not too large)
        if (responseBody.Length > 0 && responseBody.Length < 10000) // Limit to 10KB
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(responseBody).ReadToEndAsync();

            if (!string.IsNullOrEmpty(body) && response.ContentType?.Contains("application/json") == true)
            {
                var maskedBody = MaskSensitiveData(body);
                _logger.LogDebug("[{CorrelationId}] Response Body: {Body}", correlationId, maskedBody);
            }
        }
        else if (responseBody.Length >= 10000)
        {
            _logger.LogDebug("[{CorrelationId}] Response Body: [TRUNCATED - {Length} bytes]", correlationId, responseBody.Length);
        }
    }

    private static async Task<string> ReadRequestBody(HttpRequest request)
    {
        using var reader = new StreamReader(
            request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        return body;
    }

    private static string MaskSensitiveData(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;

        // Simple masking for common sensitive field patterns
        foreach (var field in SensitiveFields)
        {
            // Match JSON patterns like "password": "value" or "password":"value"
            var patterns = new[]
            {
                $"\"{field}\"\\s*:\\s*\"[^\"]*\"",
                $"\"{field}\"\\s*:\\s*'[^']*'",
            };

            foreach (var pattern in patterns)
            {
                json = System.Text.RegularExpressions.Regex.Replace(
                    json,
                    pattern,
                    $"\"{field}\": \"***MASKED***\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
        }

        return json;
    }
}

/// <summary>
/// Extension methods for RequestResponseLoggingMiddleware
/// </summary>
public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
