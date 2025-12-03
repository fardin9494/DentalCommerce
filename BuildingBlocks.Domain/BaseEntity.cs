using System;

namespace BuildingBlocks.Domain;

public abstract class BaseEntity<TId>
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;
    protected void Touch() => UpdatedAt = DateTime.UtcNow;
}

public abstract class AggregateRoot<TId> : BaseEntity<TId>
{
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
}