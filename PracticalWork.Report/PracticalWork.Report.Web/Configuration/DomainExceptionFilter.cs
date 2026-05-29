using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PracticalWork.Report.Web.Configuration;

public class DomainExceptionFilter<TAppException> : IAsyncActionFilter where TAppException : Exception
{
    protected readonly ILogger Logger;

    public DomainExceptionFilter(ILogger<DomainExceptionFilter<TAppException>> logger)
    {
        Logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resultContext = await next();
        if (HasException(resultContext))
        {
            TryHandleException(resultContext, resultContext.Exception);
        }
    }

    private static bool HasException(ActionExecutedContext context) => context.Exception != null && !context.ExceptionHandled;

    protected virtual void TryHandleException(ActionExecutedContext context, Exception exception)
    {
        if (exception is not TAppException)
            return;
        
        var problemDetails = BuildProblemDetails(exception);

        if (exception is NotFoundException)
        {
            context.Result = new NotFoundObjectResult(problemDetails);
        }
        else
        {
            context.Result = new BadRequestObjectResult(problemDetails);
        }
        context.ExceptionHandled = true;
        
        Logger.LogError(exception, "Unhandled domain exception. Transformed to Bad request (400).");
    }

    protected static ValidationProblemDetails BuildProblemDetails(Exception exception)
    {
        var exceptionName = exception.GetType().Name;
        var errorMessages = new[] { exception.Message };

        var problemDetails = new ValidationProblemDetails
        {
            Title = "Произошла ошибка во время выполнения запроса.",
            Errors = { { exceptionName, errorMessages } }
        };

        return problemDetails;
    }
}
