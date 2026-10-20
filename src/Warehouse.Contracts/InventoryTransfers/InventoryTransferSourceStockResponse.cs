namespace Warehouse.Contracts.InventoryTransfers;

public sealed record InventoryTransferSourceStockResponse(
    decimal QuantityOnHand, 
    decimal QuantityReserved, 
    decimal? QuantityAvailable);