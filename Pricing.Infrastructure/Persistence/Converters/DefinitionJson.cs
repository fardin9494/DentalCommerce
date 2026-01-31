using System.Text.Json;
using System.Text.Json.Serialization;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Infrastructure.Persistence.Converters;

public static class DefinitionJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new EligibilityDefinitionJsonConverter(),
            new BenefitDefinitionJsonConverter()
        }
    };

    internal static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}

public sealed class EligibilityDefinitionJsonConverter : JsonConverter<EligibilityDefinition>
{
    public override EligibilityDefinition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        if (!DefinitionJson.TryGetPropertyIgnoreCase(doc.RootElement, "kind", out var kindElement))
            throw new JsonException("Eligibility definition missing kind.");

        var kind = kindElement.GetString();
        var target = kind switch
        {
            "all" => typeof(AllEligibilityDefinition),
            "allOf" => typeof(AllOfEligibilityDefinition),
            "anyOf" => typeof(AnyOfEligibilityDefinition),
            "product" => typeof(ProductEligibilityDefinition),
            "category" => typeof(CategoryEligibilityDefinition),
            "brand" => typeof(BrandEligibilityDefinition),
            "tag" => typeof(TagEligibilityDefinition),
            "site" => typeof(SiteEligibilityDefinition),
            "user" => typeof(UserEligibilityDefinition),
            "seasonal" => typeof(SeasonalEligibilityDefinition),
            "bundle" => typeof(BundleEligibilityDefinition),
            "batchExpiryBefore" => typeof(BatchExpiryBeforeEligibilityDefinition),
            _ => typeof(EligibilityDefinition)
        };

        return (EligibilityDefinition?)JsonSerializer.Deserialize(doc.RootElement.GetRawText(), target, options);
    }

    public override void Write(Utf8JsonWriter writer, EligibilityDefinition value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}

public sealed class BenefitDefinitionJsonConverter : JsonConverter<BenefitDefinition>
{
    public override BenefitDefinition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        if (!DefinitionJson.TryGetPropertyIgnoreCase(doc.RootElement, "kind", out var kindElement))
            throw new JsonException("Benefit definition missing kind.");

        var kind = kindElement.GetString();
        var target = kind switch
        {
            "percentOff" => typeof(PercentOffBenefitDefinition),
            "amountOff" => typeof(AmountOffBenefitDefinition),
            "fixedPrice" => typeof(FixedPriceBenefitDefinition),
            "bundleFixedPrice" => typeof(BundleFixedPriceBenefitDefinition),
            "buyXGetY" => typeof(BuyXGetYBenefitDefinition),
            "cashbackPercent" => typeof(CashbackPercentBenefitDefinition),
            "cashbackAmount" => typeof(CashbackAmountBenefitDefinition),
            _ => typeof(BenefitDefinition)
        };

        return (BenefitDefinition?)JsonSerializer.Deserialize(doc.RootElement.GetRawText(), target, options);
    }

    public override void Write(Utf8JsonWriter writer, BenefitDefinition value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }

}
