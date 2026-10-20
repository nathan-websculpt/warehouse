namespace Warehouse.Contracts.InventoryTransfers;

public sealed record InventoryTransferLocationResponse(
    Guid WarehousePublicId, 
    string WarehouseCode, 
    string WarehouseName, 
    bool WarehouseIsActive,
    int WarehouseLocationId, 
    string LocationCode, 
    string? LocationDisplayName, 
    bool LocationIsActive);