using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using task11.Web.Middleware;

namespace task11.Web.Infrastructure;

public static class ProblemDetailsEnricher
{
    public static void Enrich(HttpContext? httpContext, ProblemDetails problemDetails)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);

        if (httpContext is not null)
        {
            string? correlationId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var cid)
                ? cid?.ToString()
                : null;

            if (!string.IsNullOrEmpty(correlationId))
            {
                problemDetails.Extensions["correlationId"] = correlationId;
            }
        }

        string? traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
        if (!string.IsNullOrEmpty(traceId))
        {
            problemDetails.Extensions["traceId"] = traceId;
        }
    }
}
