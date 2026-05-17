using System.Text.RegularExpressions;

namespace Sayiad.Api.Middleware;

public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;

    public InputSanitizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.HasFormContentType && context.Request.Form.Count > 0)
        {
            var form = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
            foreach (var key in context.Request.Form.Keys)
            {
                form[key] = SanitizeValue(context.Request.Form[key]);
            }
            var formCollection = new FormCollection(form);
            context.Request.Form = formCollection;
        }

        if (context.Request.QueryString.HasValue)
        {
            var query = System.Web.HttpUtility.ParseQueryString(context.Request.QueryString.Value ?? "");
            var sanitized = false;
            foreach (var key in query.AllKeys.Where(k => k != null))
            {
                var original = query[key!] ?? "";
                var cleaned = Regex.Replace(original, @"<[^>]*>", string.Empty);
                if (cleaned != original)
                {
                    query[key!] = cleaned;
                    sanitized = true;
                }
            }
            if (sanitized)
            {
                context.Request.QueryString = new QueryString("?" + query.ToString());
            }
        }

        await _next(context);
    }

    private static string SanitizeValue(string value)
    {
        return Regex.Replace(value, @"<[^>]*>", string.Empty);
    }
}
