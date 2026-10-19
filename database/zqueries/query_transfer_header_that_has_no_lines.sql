USE WarehouseDev;
GO

SELECT
    transfer.InventoryTransferPublicId,
    transfer.TransferStatusCode
FROM dbo.InventoryTransfers AS transfer
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.InventoryTransferLines AS line
    WHERE line.TenantId = transfer.TenantId
      AND line.InventoryTransferId = transfer.InventoryTransferId
);
GO