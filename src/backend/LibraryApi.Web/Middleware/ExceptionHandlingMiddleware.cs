using System.Text.Json;
using LibraryApi.Domain.Exceptions;

namespace LibraryApi.Web.Middleware;

public class ExceptionHandlingMiddleware
{
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
        catch (NotFoundException ex)
        {
            await Write(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ForbiddenException ex)
        {
            await Write(context, StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (ConflictException ex)
        {
            await Write(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (DomainException ex)
        {
            await Write(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await Write(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static Task Write(HttpContext ctx, int status, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new { error = message });
        return ctx.Response.WriteAsync(payload);
    }
}
