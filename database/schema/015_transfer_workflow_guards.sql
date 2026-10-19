SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- These are content-integrity guards, not caller authorization.
-- Administrators remain responsible for controlling DDL and access to the posting workflow.

CREATE OR ALTER TRIGGER dbo.TR_InventoryTransfers_ProtectContent
ON dbo.InventoryTransfers
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- identity and pagination keys never change, even while a transfer is editable
    IF EXISTS
    (
        SELECT 1
        FROM deleted AS old
        INNER JOIN inserted AS changed
            ON changed.InventoryTransferId = old.InventoryTransferId
        WHERE changed.TenantId <> old.TenantId
           OR changed.InventoryTransferPublicId <> old.InventoryTransferPublicId
           OR changed.CreatedAtUtc <> old.CreatedAtUtc
    )
        THROW 51016, N'Transfer identity and creation time cannot be changed.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM deleted AS old
        LEFT JOIN inserted AS changed 
            ON changed.InventoryTransferId = old.InventoryTransferId
        WHERE old.TransferStatusCode = 'RECEIVED'
            OR 
            (
                old.TransferStatusCode = 'APPROVED'
                    AND 
                    (
                        changed.InventoryTransferId IS NULL
                        OR changed.TransferStatusCode <> 'RECEIVED'
                        OR changed.UpdatedAtUtc IS NULL
                        OR changed.SourceLocationId <> old.SourceLocationId
                        OR changed.DestinationLocationId <> old.DestinationLocationId
                    )
            )
    )
        THROW 51016, N'Approved and received transfer contents are immutable. Only approved-to-received completion is permitted.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS changed
        LEFT JOIN deleted AS old
            ON old.InventoryTransferId = changed.InventoryTransferId
        WHERE changed.TransferStatusCode = 'RECEIVED'
          AND (old.InventoryTransferId IS NULL OR old.TransferStatusCode <> 'APPROVED')
    )
        THROW 51016, N'A received transfer must transition from approved through the controlled workflow.', 1; 
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_InventoryTransferLines_ProtectContent
ON dbo.InventoryTransferLines
AFTER INSERT, UPDATE, DELETE 
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Parents TABLE (
        TenantId INT, 
        InventoryTransferId INT, 
        TransferStatusCode VARCHAR(20), 
        PRIMARY KEY (TenantId, InventoryTransferId)
    );

    -- Both sides matter when a line is reassigned.
    -- UPDLOCK/HOLDLOCK forces a current locking read even with READ_COMMITTED_SNAPSHOT enabled.
    -- A racing approval cannot commit alongside an unnoticed line edit.

    INSERT INTO @Parents (
        TenantId,
        InventoryTransferId,
        TransferStatusCode
    )
    SELECT parent.TenantId, parent.InventoryTransferId, parent.TransferStatusCode
    FROM dbo.InventoryTransfers AS parent 
        WITH (UPDLOCK, HOLDLOCK) 
    INNER JOIN
    (
        SELECT TenantId, InventoryTransferId 
        FROM inserted
        UNION
        SELECT TenantId, InventoryTransferId 
        FROM deleted
    ) AS affected 
        ON affected.TenantId = parent.TenantId 
            AND affected.InventoryTransferId = parent.InventoryTransferId;

    IF EXISTS 
    (
        SELECT 1
        FROM @Parents
        WHERE TransferStatusCode IN ('APPROVED', 'RECEIVED')
    )
        THROW 51016, N'Lines belonging to approved or received transfers cannot be inserted, changed, removed, or reassigned.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS changed
        INNER JOIN deleted AS old 
            ON old.InventoryTransferLineId = changed.InventoryTransferLineId
        WHERE changed.CreatedAtUtc <> old.CreatedAtUtc
    )
        THROW 51016, N'Transfer line creation time cannot be changed.', 1;

    -- SQL Server advances RowVersion when this parent row is updated
    UPDATE parent SET UpdatedAtUtc = SYSUTCDATETIME()
    FROM dbo.InventoryTransfers AS parent
    INNER JOIN @Parents AS affected
        ON affected.TenantId = parent.TenantId
            AND affected.InventoryTransferId = parent.InventoryTransferId;
END;
GO
