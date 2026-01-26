using BlogApi.Domain.Exceptions;
using FluentValidation;
using System.Text.Json;
using System.IO;

namespace BlogApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        object responseModel;
        int statusCode;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = StatusCodes.Status400BadRequest;
                responseModel = new { Errors = validationException.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) };
                break;

            case EntityNotFoundException:
            case KeyNotFoundException:
            case FileNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                responseModel = new { Message = exception.Message };
                break;

            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status403Forbidden;
                responseModel = new { Message = "Forbidden" };
                break;
                
            case AccessDeniedException:
                statusCode = StatusCodes.Status403Forbidden;
                responseModel = new { Message = exception.Message };
                break;

            case DomainException:
            case ArgumentException:
                statusCode = StatusCodes.Status400BadRequest;
                responseModel = new { Message = exception.Message };
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                responseModel = new { Message = "Internal Server Error" };
                
                // Show detailed error in Development or Testing
                if (_env.IsEnvironment("Testing") || _env.IsDevelopment())
                {
                    responseModel = new { Message = exception.Message, StackTrace = exception.StackTrace };
                }
                break;
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(responseModel));
    }
}
