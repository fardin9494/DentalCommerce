using MediatR;
namespace Inventory.Application.Features.Transfers.Commands;
public sealed record AddTransferLineCommand(Guid TransferId, Guid ProductId, Guid? VariantId, decimal Qty) : IRequest<Guid>;