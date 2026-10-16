using Warehouse.Contracts.InventoryTransfers;

namespace Warehouse.Contracts.InventoryTransferLines;

public sealed record InventoryTransferLineResponse(
    DateTime CreatedAtUtc,
    decimal QuantityRequested,
    int InventoryTransferLineId,
    int InventoryLotId,
    InventoryTransferResponse InventoryTransfer);
