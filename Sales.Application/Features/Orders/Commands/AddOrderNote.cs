using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Commands;

public sealed record AddOrderNoteCommand(
    Guid OrderId,
    AddOrderNoteRequest Request
) : IRequest<AddOrderNoteResult>;

public sealed class AddOrderNoteRequest
{
    public string Note { get; init; } = null!;
    public string? CreatedBy { get; init; }
    public bool IsInternal { get; init; } = false;
}

public sealed record AddOrderNoteResult(
    Guid NoteId,
    DateTime CreatedAt);

public sealed class AddOrderNoteHandler : IRequestHandler<AddOrderNoteCommand, AddOrderNoteResult>
{
    private readonly ISalesDbContext _db;

    public AddOrderNoteHandler(ISalesDbContext db)
    {
        _db = db;
    }

    public async Task<AddOrderNoteResult> Handle(AddOrderNoteCommand cmd, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Timeline)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct);

        if (order is null)
            throw new InvalidOperationException($"Order {cmd.OrderId} not found.");

        var note = OrderNote.Create(
            cmd.OrderId,
            cmd.Request.Note,
            cmd.Request.CreatedBy,
            cmd.Request.IsInternal);

        _db.OrderNotes.Add(note);
        order.LogNoteAdded(note.Id, note.CreatedBy, note.IsInternal, note.CreatedAt);
        OrderCommandHelpers.EnsureLatestTimelineTracked(_db, order);
        await _db.SaveChangesAsync(ct);

        return new AddOrderNoteResult(note.Id, note.CreatedAt);
    }
}

public sealed class AddOrderNoteValidator : AbstractValidator<AddOrderNoteCommand>
{
    public AddOrderNoteValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Request.Note)
            .NotEmpty()
            .MaximumLength(2000)
            .WithMessage("Note cannot exceed 2000 characters.");
    }
}
