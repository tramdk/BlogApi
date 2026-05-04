using FloraCore.Application.Common.Models;
using FloraCore.Domain.Exceptions;
using FluentValidation;
using System.Text.Json;

namespace FloraCore.Middleware;

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
        
        ApiResponse<object> apiResponse;
        int statusCode;
        
        switch (exception)
        {
            case ValidationException validationException:
                statusCode = StatusCodes.Status400BadRequest;
                apiResponse = ApiResponse<object>.FailureResult(
                    validationException.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}").ToList(),
                    "Validation failed."
                );
                break;

            case EntityNotFoundException:
            case KeyNotFoundException:
            case FileNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                apiResponse = new ApiResponse<object> { Success = false, Message = exception.Message };
                break;

            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status401Unauthorized;
                apiResponse = new ApiResponse<object> { Success = false, Message = "Unauthorized access." };
                break;
                
            case AccessDeniedException:
                statusCode = StatusCodes.Status403Forbidden;
                apiResponse = new ApiResponse<object> { Success = false, Message = exception.Message };
                break;

            case DomainException:
            case ArgumentException:
                statusCode = StatusCodes.Status400BadRequest;
                apiResponse = new ApiResponse<object> { Success = false, Message = exception.Message };
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                var message = (_env.IsEnvironment("Testing") || _env.IsDevelopment()) 
                    ? exception.Message 
                    : "An internal server error occurred.";
                
                apiResponse = new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = message,
                    Errors = (_env.IsEnvironment("Testing") || _env.IsDevelopment()) 
                        ? new List<string> { exception.StackTrace ?? "" } 
                        : null
                };
                break;
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        }));
    }
}
