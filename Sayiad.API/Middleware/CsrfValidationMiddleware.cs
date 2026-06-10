using Microsoft.AspNetCore.Antiforgery;

namespace Sayiad.Api.Middleware;

public class CsrfValidationMiddleware : IMiddleware
{
    private readonly IAntiforgery _antiforgery;

    public CsrfValidationMiddleware(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (ShouldValidate(context.Request.Method))
        {
            var endpoint = context.GetEndpoint();
            var ignore = endpoint?.Metadata?.GetMetadata<IgnoreAntiforgeryTokenAttribute>() != null;

            if (!ignore)
            {
                if (!await _antiforgery.IsRequestValidAsync(context))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { message = "Invalid or missing anti-forgery token." });
                    return;
                }
            }
        }

        await next(context);
    }

    private static bool ShouldValidate(string method) => method switch
    {
        "POST" or "PUT" or "PATCH" or "DELETE" => true,
        _ => false,
    };
}
