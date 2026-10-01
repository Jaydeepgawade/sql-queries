using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EquityAudit.Api.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken token)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            return false;

        var invalid = exception is ValidationException ||
            exception is SqlException { Number: >= 50001 and <= 50004 };
        var status = invalid ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
        if (!invalid) logger.LogError(exception, "Dashboard request failed. Trace {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = invalid ? "Invalid dashboard filter" : "Dashboard request failed",
            Detail = invalid ? exception.Message : "Unable to load dashboard data. Please try again.",
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, token);
        return true;
    }
}
