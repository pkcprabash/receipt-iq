using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.ErrorHandling;

// A safety net for exceptions that escape an endpoint's own handling (DB connection
// failures, bugs) — expected error cases (bad input, not found, conflicts) are still
// returned directly by each endpoint and never reach this. Logs, then responds with
// ProblemDetails rather than leaking a stack trace to the client.
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            // The trace id ties this response back to the logged exception without
            // exposing exception details outside Development.
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        };

        if (environment.IsDevelopment())
        {
            problemDetails.Detail = exception.ToString();
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
