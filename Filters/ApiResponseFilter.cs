using BlogApi.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BlogApi.Filters;

public class ApiResponseFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult)
        {
            // Don't wrap if it's already an ApiResponse
            var valueType = objectResult.Value?.GetType();
            if (valueType == null || (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(ApiResponse<>)))
            {
                await next();
                return;
            }

            // Create the wrapper
            var apiResponse = new ApiResponse<object>
            {
                Success = objectResult.StatusCode is >= 200 and < 300,
                Data = objectResult.Value,
                Message = GetDefaultMessage(objectResult.StatusCode)
            };

            // Update the result
            objectResult.Value = apiResponse;
        }

        await next();
    }

    private static string? GetDefaultMessage(int? statusCode)
    {
        return statusCode switch
        {
            200 => "Request processed successfully.",
            201 => "Resource created successfully.",
            204 => "No content.",
            400 => "Bad request.",
            401 => "Unauthorized.",
            403 => "Forbidden.",
            404 => "Resource not found.",
            _ => null
        };
    }
}
