using BuildingBlocks.Domain;

namespace Sales.Domain.Orders;

public sealed class OrderNote : BaseEntity<Guid>
{
    public Guid OrderId { get; private set; }
    public string Note { get; private set; } = null!;
    public string? CreatedBy { get; private set; } // User ID or username who created the note
    public bool IsInternal { get; private set; } // If true, only visible to admins/staff

    private OrderNote() { }

    public static OrderNote Create(
        Guid orderId,
        string note,
        string? createdBy = null,
        bool isInternal = false,
        DateTime? createdAt = null)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId required.", nameof(orderId));
        
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Note text is required.", nameof(note));

        var ts = createdAt ?? DateTime.UtcNow;
        if (ts.Kind != DateTimeKind.Utc)
            ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);

        return new OrderNote
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Note = note.Trim(),
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? null : createdBy.Trim(),
            IsInternal = isInternal,
            CreatedAt = ts,
            UpdatedAt = ts
        };
    }

    public void UpdateNote(string newNote)
    {
        if (string.IsNullOrWhiteSpace(newNote))
            throw new ArgumentException("Note text is required.", nameof(newNote));

        Note = newNote.Trim();
        Touch();
    }
}
