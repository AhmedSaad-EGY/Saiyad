using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sayiad.Api.Middleware;

public record ApiErrorResponse(int StatusCode, string Message, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<string>? Errors = null)
{
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
}
