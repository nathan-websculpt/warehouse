namespace Warehouse.Contracts.Warehouses;

public sealed record WarehouseResponse(
    Guid WarehousePublicId,
    string WarehouseCode,
    string WarehouseName,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);