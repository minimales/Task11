using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Results;

namespace task11.Web.Infrastructure;

public class ValidationProblemResultFactory : IFluentValidationAutoValidationResultFactory
{
    public IActionResult CreateActionResult(
        ActionExecutingContext context,
        ValidationProblemDetails? validationProblemDetails)
    {
        ArgumentNullException.ThrowIfNull(context);

        ValidationProblemDetails problemDetails = validationProblemDetails ?? new ValidationProblemDetails();
        problemDetails.Type = "about:blank";
        problemDetails.Title = "Validation failed";
        problemDetails.Status = StatusCodes.Status400BadRequest;

        ProblemDetailsEnricher.Enrich(context.HttpContext, problemDetails);

        return new JsonResult(problemDetails)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentType = "application/problem+json"
        };
    }
}
