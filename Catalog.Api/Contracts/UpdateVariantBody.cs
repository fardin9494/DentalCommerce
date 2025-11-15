namespace Catalog.Api.Contracts;

public sealed record UpdateVariantBody(
    string VariantValue,
    string Sku,
    bool IsActive
);

