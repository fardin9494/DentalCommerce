namespace Identity.Application.Common.Permissions;

public sealed record PermissionDefinition(
    string Key,
    string Title,
    string Description,
    string Context);
