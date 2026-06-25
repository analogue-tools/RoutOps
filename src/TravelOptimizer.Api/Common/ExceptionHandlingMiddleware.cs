using System.Text.Json;
using FluentValidation;
using TravelOptimizer.Domain.Exceptions;

namespace TravelOptimizer.Api.Common;

/// <summary>
/// Translates domain/validation exceptions into consistent HTTP responses (ERRORS.md): 404 for
/// NotFound, 400 for BadRequest/validation, 500 otherwise.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (BadRequestException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToArray();
            await WriteAsync(context, StatusCodes.Status400BadRequest, "Validation failed.", errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string message, string[]? errors = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new { message, errors });
        await context.Response.WriteAsync(payload);
    }
}
