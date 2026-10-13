
namespace Warehouse.Contracts.InventoryTransferLines;

// dbo.CompleteInventoryTransfer has two result sets
public sealed record CompleteInventoryTransferResponse(
    InventoryTransferCompletionSummaryResponse Summary,
    IReadOnlyList<InventoryTransferMovementResponse> Movements);

public sealed record InventoryTransferCompletionSummaryResponse(
    Guid InventoryTransferPublicId,
    string TransferStatusCode,
    DateTime CompletedAtUtc,
    byte[] RowVersion,
    int LineCount,
    bool IsReplay);

public sealed record InventoryTransferMovementResponse(
    int InventoryTransferLineId,
    string SKU,
    string LotNumber,
    string UnitOfMeasureCode,
    decimal QuantityMoved,
    int InventoryMovementId,
    DateTime MovementCreatedAtUtc);