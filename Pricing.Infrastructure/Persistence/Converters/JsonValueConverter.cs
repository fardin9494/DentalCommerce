using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Pricing.Infrastructure.Persistence.Converters;

public sealed class JsonValueConverter<T> : ValueConverter<T, string>
{
    public JsonValueConverter(JsonSerializerOptions options)
        : base(
            v => JsonSerializer.Serialize(v, options),
            v => JsonSerializer.Deserialize<T>(v, options)!)
    {
    }
}

public sealed class JsonValueComparer<T> : ValueComparer<T>
{
    public JsonValueComparer(JsonSerializerOptions options)
        : base(
            (l, r) => JsonSerializer.Serialize(l, options) == JsonSerializer.Serialize(r, options),
            v => JsonSerializer.Serialize(v, options).GetHashCode(),
            v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, options), options)!)
    {
    }
}
