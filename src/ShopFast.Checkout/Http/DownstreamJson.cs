using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShopFast.Checkout.Http;

internal static class DownstreamJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
