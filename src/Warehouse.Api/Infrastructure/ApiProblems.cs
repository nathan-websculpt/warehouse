using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Warehouse.Contracts.Errors;

namespace Warehouse.Api.Infrastructure;

/*
        shared error responses/handling -- uses Core's ProblemDetails format, adding two fields
            code:      identifier like INVALID_CURSOR or TRANSFER_VERSION_CONFLICT
            traceId:   reference for finding the associated request in diagnostics


        ApiProblems has shared helper methods:
            Normalize()          fills in missing error info and attaches diagnostic details
            Result()             gives controllers a way to create error responses
            GetSqlOperation()    determines whether the endpoint performs a transfer operation, a stock availability operation, or another operation


        ApiProblemFilter standardizes controller results before they are sent

        ApiExceptionHandler converts unhandled exceptions into error responses
 */

public static class ApiProblems
{
    // exception middleware clears the current endpoint -- its feature retains the original metadata
    public static SqlOperation GetSqlOperation(HttpContext context) =>
        (context.GetEndpoint() ?? context.Features.Get<IExceptionHandlerFeature>()?.Endpoint)?.Metadata.GetMetadata<SqlOperationAttribute>()?.Operation ?? SqlOperation.Other;

    public static void Normalize(ProblemDetails problem, HttpContext context)
    {
        var status = problem.Status ?? 500;
        problem.Status = status;
        problem.Title ??= status >= 500 ? "The request could not be completed." : "The request was rejected.";
        problem.Detail ??= problem.Title;

        if (!problem.Extensions.ContainsKey("code"))
        {
            problem.Extensions["code"] = status switch
                {
                    400 => ErrorCodes.Validation,
                    404 => ErrorCodes.NotFound,
                    _ => status >= 500 ?
                        ErrorCodes.Unexpected :
                        $"HTTP_{status}"
                };
        }
          
        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        problem.Type = $"urn:warehouse:problem:{problem.Extensions["code"]}";
    }

    public static ObjectResult Result(int status, string code, string detail) => 
        new(new ProblemDetails
        {
            Status = status,
            Detail = detail,
            Extensions = { ["code"] = code }
        })
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
}

public sealed class ApiProblemFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is StatusCodeResult { StatusCode: >= 400 } statusResult)
        {
            context.Result = new ObjectResult(
                new ProblemDetails 
                { Status = statusResult.StatusCode })
            { 
                StatusCode = statusResult.StatusCode
            };
        }

        if (context.Result is ObjectResult { Value: ProblemDetails problem } result)
        {
            ApiProblems.Normalize(problem, context.HttpContext);
            result.ContentTypes.Clear();
            result.ContentTypes.Add("application/problem+json");
        }
    }
    public void OnResultExecuted(ResultExecutedContext context) { }
}

public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            return false;

        var operation = ApiProblems.GetSqlOperation(context);
        var failure = exception is SqlException sql ?
            SqlErrorMapper.Map(operation, sql.Number) :
            new SqlFailure(500, ErrorCodes.Unexpected, "An unexpected error occurred. Contact support with the reference.");

        logger.Log(failure.Status >= 500 ?
            LogLevel.Error :
            LogLevel.Warning, exception, "API request failed with {Code}.", failure.Code);

        var problem = new ProblemDetails
        {
            Status = failure.Status,
            Title = failure.Title,

            Detail = failure.Status < 500 && exception is SqlException ?
            exception.Message :
            failure.Title,

            Extensions = { ["code"] = failure.Code }
        };

        ApiProblems.Normalize(problem, context);
        context.Response.StatusCode = failure.Status;

        if (!await problems.TryWriteAsync(
            new ProblemDetailsContext 
            {
                HttpContext = context,
                ProblemDetails = problem,
                Exception = exception
            }))
        {
            await context.Response.WriteAsJsonAsync(
                problem,
                (System.Text.Json.JsonSerializerOptions?)null,
                "application/problem+json",
                cancellationToken);
        }

            
        return true;
    }
}
