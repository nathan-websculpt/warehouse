namespace Warehouse.Contracts.InventoryTransfers;

public sealed record InventoryTransferDetailResponse(
    InventoryTransferResponse Transfer,
    InventoryTransferLocationResponse Source,
    InventoryTransferLocationResponse Destination,
    IReadOnlyList<InventoryTransferDetailLineResponse> Lines);