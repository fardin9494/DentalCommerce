namespace Pricing.Application.Abstractions;

public interface IInventoryBatchInfoGateway
{
    Task<BatchInfo?> GetBatchInfoAsync(string skuId, Guid batchId, CancellationToken ct);
}

public sealed record BatchInfo(Guid BatchId, string SkuId, DateTime? ExpiryDate);
