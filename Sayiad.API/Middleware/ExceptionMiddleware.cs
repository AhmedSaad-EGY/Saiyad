using System.Net;

namespace Sayiad.Api.Middleware;

public class ExceptionMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            if (!context.Response.HasStarted)
                await WriteErrorResponse(context, HttpStatusCode.Unauthorized, "Authentication failed. Please log in again.");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            if (!context.Response.HasStarted)
                await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            if (!context.Response.HasStarted)
                await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument");
            if (!context.Response.HasStarted)
                await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid format");
            if (!context.Response.HasStarted)
                await WriteErrorResponse(context, HttpStatusCode.BadRequest, "Invalid request format.");
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Not supported");
            if (!context.Response.HasStarted)
                await WriteErrorResponse(context, HttpStatusCode.BadRequest, "Operation is not supported.");
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Request cancelled by client");
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499;
                return;
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error");
            if (!context.Response.HasStarted)
                await WriteErrorResponse(context, HttpStatusCode.Conflict, "A data conflict occurred. Please retry.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            if (!context.Response.HasStarted)
                await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var error = new ApiErrorResponse((int)statusCode, message);
        await context.Response.WriteAsync(error.ToJson());
    }
}
