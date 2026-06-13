using System.Text.Json;
using FluentValidation;
using task11.ApplicationCore;
using task11.ApplicationCore.Models;

namespace task11.Web.Middleware;

/// <summary>
/// Translates exceptions into RFC 7807 problem responses:
/// NotFound→404, Forbidden→403, Conflict→409, FxUnavailable→503,
/// ValidationException→400, anything else→500. Secrets are never included.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private const int _serverErrorThreshold = 500;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            FxUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Currency service unavailable"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (status >= _serverErrorThreshold)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Request failed ({Status}): {Message}", status, exception.Message);
        }

        string? correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var cid)
            ? cid?.ToString()
            : null;

        var response = new ErrorModel
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = title,
            Status = status,
            Detail = status >= _serverErrorThreshold ? "An unexpected error occurred." : exception.Message,
            Instance = context.Request.Path,
            CorrelationId = correlationId
        };

        if (exception is ValidationException validationException)
        {
            response.Errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        if (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
        }
    }
}
