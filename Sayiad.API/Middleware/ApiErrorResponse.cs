using System.Text.Json;

namespace Sayiad.Api.Middleware;

public record ApiErrorResponse(int StatusCode, string Message, List<string>? Errors = null)
{
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
}
