IF NOT EXISTS 
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.InventoryTransfers') 
        AND name = N'IX_InventoryTransfers_Page'
)
BEGIN
    CREATE INDEX IX_InventoryTransfers_Page
        ON dbo.InventoryTransfers 
        (
            CreatedAtUtc, 
            InventoryTransferId
        );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.InventoryTransfers')
        AND name = N'IX_InventoryTransfers_TenantPage'
)
BEGIN
    CREATE INDEX IX_InventoryTransfers_TenantPage
        ON dbo.InventoryTransfers
        (
            TenantId, 
            CreatedAtUtc, 
            InventoryTransferId
        );
END;

IF NOT EXISTS 
(
    SELECT 1 
    FROM sys.indexes 
    WHERE object_id = OBJECT_ID(N'dbo.InventoryTransferLines')
        AND name = N'IX_InventoryTransferLines_Page'
)
BEGIN
    CREATE INDEX IX_InventoryTransferLines_Page
        ON dbo.InventoryTransferLines
        (
            CreatedAtUtc, 
            InventoryTransferLineId
        );
END;

IF NOT EXISTS 
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.InventoryTransferLines')
        AND name = N'IX_InventoryTransferLines_TenantPage'
)
BEGIN
    CREATE INDEX IX_InventoryTransferLines_TenantPage
        ON dbo.InventoryTransferLines
        (
            TenantId,
            CreatedAtUtc,
            InventoryTransferLineId
        );
END;
GO
