using Serilog.Context;

namespace task11.Web.Middleware;

/// <summary>
/// Establishes a correlation id for the request: reads <c>X-Correlation-Id</c> or generates one,
/// stores it in <see cref="HttpContext.Items"/>, pushes <c>CorrelationId</c> into the Serilog log
/// scope, and echoes the id back on the response. The <c>UserId</c> scope is added later by
/// <see cref="RequestResponseLoggingMiddleware"/>, which runs after authentication.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemsKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
                               && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Items[ItemsKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
