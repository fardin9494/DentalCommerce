using Identity.Domain.Admin;

namespace Identity.Application.Models;

public sealed record InventoryPermissionItemDto(
    string Key,
    string Title,
    string Description,
    bool Granted);

public sealed record InventoryPermissionDto(
    Guid UserId,
    bool IsSuperAdmin,
    PermissionUiPolicy UiPolicy,
    IReadOnlyList<InventoryPermissionItemDto> Items);

public sealed class UpdateInventoryPermissionsRequest
{
    public PermissionUiPolicy? UiPolicy { get; init; }
    public IReadOnlyList<PermissionGrant> Permissions { get; init; } = Array.Empty<PermissionGrant>();
}

public sealed record CatalogPermissionItemDto(
    string Key,
    string Title,
    string Description,
    bool Granted);

public sealed record CatalogPermissionDto(
    Guid UserId,
    bool IsSuperAdmin,
    PermissionUiPolicy UiPolicy,
    IReadOnlyList<CatalogPermissionItemDto> Items);

public sealed class UpdateCatalogPermissionsRequest
{
    public PermissionUiPolicy? UiPolicy { get; init; }
    public IReadOnlyList<PermissionGrant> Permissions { get; init; } = Array.Empty<PermissionGrant>();
}

public sealed record PricingPermissionItemDto(
    string Key,
    string Title,
    string Description,
    bool Granted);

public sealed record PricingPermissionDto(
    Guid UserId,
    bool IsSuperAdmin,
    PermissionUiPolicy UiPolicy,
    IReadOnlyList<PricingPermissionItemDto> Items);

public sealed class UpdatePricingPermissionsRequest
{
    public PermissionUiPolicy? UiPolicy { get; init; }
    public IReadOnlyList<PermissionGrant> Permissions { get; init; } = Array.Empty<PermissionGrant>();
}

public sealed record SalesPermissionItemDto(
    string Key,
    string Title,
    string Description,
    bool Granted);

public sealed record SalesPermissionDto(
    Guid UserId,
    bool IsSuperAdmin,
    PermissionUiPolicy UiPolicy,
    IReadOnlyList<SalesPermissionItemDto> Items);

public sealed class UpdateSalesPermissionsRequest
{
    public PermissionUiPolicy? UiPolicy { get; init; }
    public IReadOnlyList<PermissionGrant> Permissions { get; init; } = Array.Empty<PermissionGrant>();
}

public sealed record PermissionGrant(string Key, bool Granted);
