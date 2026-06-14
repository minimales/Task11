using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Extensions;
using task11.Web.Infrastructure.Logging;
using Serilog.Context;

namespace task11.Web.Middleware;

public class RequestResponseLoggingMiddleware
{
    private const int _defaultMaxBodyBytes = 32 * 1024;

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly int _maxBodyBytes;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configuration);

        _next = next;
        _logger = logger;
        _maxBodyBytes = configuration.GetValue<int?>("Logging:MaxBodyBytes") ?? _defaultMaxBodyBytes;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string userId = context.User?.FindFirst("sub")?.Value
                        ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? "anonymous";

        using IDisposable userScope = LogContext.PushProperty("UserId", userId);

        string requestBody = await ReadRequestBodyAsync(context.Request);
        string url = context.Request.GetEncodedUrl();

        _logger.LogInformation(
            "HTTP request {Method} {Url} Body={Body}",
            context.Request.Method,
            url,
            LogSanitizer.Sanitize(requestBody, _maxBodyBytes));

        Stream originalBody = context.Response.Body;
        await using MemoryStream buffer = new MemoryStream();
        context.Response.Body = buffer;

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            buffer.Seek(0, SeekOrigin.Begin);
            string responseBody = await new StreamReader(buffer).ReadToEndAsync();
            buffer.Seek(0, SeekOrigin.Begin);

            await buffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;

            _logger.LogInformation(
                "HTTP response {StatusCode} Body={Body} ElapsedMs={ElapsedMs}",
                context.Response.StatusCode,
                LogSanitizer.Sanitize(responseBody, _maxBodyBytes),
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();

        if (request.ContentLength is null or 0)
        {
            return string.Empty;
        }

        request.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new StreamReader(request.Body, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }
}
