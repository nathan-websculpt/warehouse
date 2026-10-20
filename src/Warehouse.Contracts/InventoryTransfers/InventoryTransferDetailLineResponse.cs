namespace Warehouse.Contracts.InventoryTransfers;

public sealed record InventoryTransferDetailLineResponse(
    int InventoryTransferLineId, 
    int InventoryLotId, 
    decimal QuantityRequested, 
    DateTime CreatedAtUtc,
    Guid ProductPublicId, 
    string SKU, 
    string ProductName, 
    bool ProductIsActive,
    string LotNumber, 
    DateOnly? ExpirationDate, 
    string UnitOfMeasureCode,
    InventoryTransferSourceStockResponse? SourceStock);