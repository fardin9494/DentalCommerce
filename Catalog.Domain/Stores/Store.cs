namespace Catalog.Domain.Stores;

public sealed class Store
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Domain { get; private set; }

    private Store() { }
    public static Store Create(string name, string? domain = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Domain = string.IsNullOrWhiteSpace(domain) ? null : domain.Trim()
        };
}