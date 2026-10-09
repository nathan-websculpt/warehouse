SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
    Posts the APPROVED -> RECEIVED transfer in one transaction.
    Moves the exact lots requested - existing reservations are never changed.
    A verified RECEIVED transfer returns its result without writes.

    Call without an existing transaction and with IMPLICIT_TRANSACTIONS OFF.
    Approved/received headers and line sets must be immutable outside their
    controlled workflow. A header RowVersion does not version its child lines.

    Returns two result sets, after commit:
      1. Transfer summary, including RowVersion and IsReplay.
      2. Durable movement details, ordered by InventoryTransferLineId.

    SERIALIZABLE protects eligibility data, line sets, movement history, and
    missing balance keys. UPDLOCK reserves the header and stock for posting.
    Deadlocks remain possible; retry SQL Server error 1205 as a whole call.

*/

CREATE OR ALTER PROCEDURE dbo.CompleteInventoryTransfer
    @TenantId INT,
    @InventoryTransferPublicId UNIQUEIDENTIFIER,
    @ExpectedRowVersion BINARY(8)
AS
BEGIN
    SET NOCOUNT ON;

    -- NOTE: RAISERROR deliberately avoids honoring a caller's XACT_ABORT setting...
    --      these boundary checks must not roll back a transaction we do not own
    IF @@TRANCOUNT <> 0
    BEGIN
        RAISERROR(N'CompleteInventoryTransfer requires no existing transaction.', 16, 1);
        RETURN;
    END;

    IF (@@OPTIONS & 2) = 2
    BEGIN
        RAISERROR(N'CompleteInventoryTransfer requires IMPLICIT_TRANSACTIONS OFF.', 16, 1);
        RETURN;
    END;

    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

    IF @TenantId IS NULL
        THROW 51000, N'TenantId is required.', 1;

    IF @InventoryTransferPublicId IS NULL
        THROW 51000, N'InventoryTransferPublicId is required.', 1;

    IF @ExpectedRowVersion IS NULL
        THROW 51000, N'ExpectedRowVersion is required.', 1;

    DECLARE
        @InventoryTransferId INT,
        @CurrentStatus VARCHAR(20),
        @CurrentRowVersion BINARY(8),
        @SourceLocationId INT,
        @DestinationLocationId INT,
        @CompletedAtUtc DATETIME2(7),
        @OperationAtUtc DATETIME2(7),
        @MinimumExpirationDate DATE,
        @TenantIsActive BIT,
        @SourceLocationIsActive BIT,
        @DestinationLocationIsActive BIT,
        @SourceWarehouseIsActive BIT,
        @DestinationWarehouseIsActive BIT,
        @LineCount INT,
        @DestinationUpdatedCount INT,
        @InvalidLineId INT,
        @ErrorMessage NVARCHAR(2048),
        @IsReplay BIT = 0;

    DECLARE @Lines TABLE
    (
        InventoryTransferLineId INT NOT NULL PRIMARY KEY,
        InventoryLotId INT NOT NULL UNIQUE,
        QuantityRequested DECIMAL(19,4) NOT NULL,
        SKU NVARCHAR(100) NOT NULL,
        LotNumber NVARCHAR(100) NOT NULL,
        UnitOfMeasureCode NVARCHAR(3) NOT NULL,
        ProductIsActive BIT NOT NULL,
        ExpirationDate DATE NULL
    );

    DECLARE @Movements TABLE
    (
        InventoryMovementId INT NOT NULL PRIMARY KEY,
        TenantId INT NOT NULL,
        InventoryTransferLineId INT NOT NULL,
        InventoryLotId INT NOT NULL,
        Quantity DECIMAL(19,4) NOT NULL,
        SourceLocationId INT NOT NULL,
        DestinationLocationId INT NOT NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL
    );

    DECLARE @Stock TABLE
    (
        WarehouseLocationId INT NOT NULL,
        InventoryLotId INT NOT NULL,
        InventoryBalanceId INT NULL,
        QuantityOnHand DECIMAL(19,4) NULL,
        QuantityReserved DECIMAL(19,4) NULL,
        PRIMARY KEY (WarehouseLocationId, InventoryLotId)
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        -- serialize calls for this transfer before deciding completion/replay
        SELECT
            @InventoryTransferId = transfer.InventoryTransferId,
            @CurrentStatus = transfer.TransferStatusCode,
            @CurrentRowVersion = transfer.RowVersion,
            @SourceLocationId = transfer.SourceLocationId,
            @DestinationLocationId = transfer.DestinationLocationId,
            @CompletedAtUtc = transfer.UpdatedAtUtc
        FROM dbo.InventoryTransfers AS transfer WITH (UPDLOCK)
        WHERE transfer.TenantId = @TenantId
          AND transfer.InventoryTransferPublicId = @InventoryTransferPublicId;

        IF @InventoryTransferId IS NULL
            THROW 51001, N'Transfer not found for this tenant.', 1;

        -- a replay accepts the caller's original precompletion version
        IF @CurrentStatus <> 'RECEIVED'
        BEGIN
            IF @CurrentStatus <> 'APPROVED'
                THROW 51002, N'Transfer must be APPROVED before completion.', 1;

            IF @CurrentRowVersion <> @ExpectedRowVersion
                THROW 51003, N'The transfer has changed. Refresh its RowVersion.', 1;
        END;

        INSERT INTO @Lines
        (
            InventoryTransferLineId, InventoryLotId, QuantityRequested,
            SKU, LotNumber, UnitOfMeasureCode, ProductIsActive, ExpirationDate
        )
        SELECT
            line.InventoryTransferLineId,
            line.InventoryLotId,
            line.QuantityRequested,
            product.SKU,
            lot.LotNumber,
            unit.UnitOfMeasureCode,
            product.IsActive,
            lot.ExpirationDate
        FROM dbo.InventoryTransferLines AS line
        INNER JOIN dbo.InventoryLots AS lot
            ON lot.TenantId = line.TenantId
           AND lot.InventoryLotId = line.InventoryLotId
        INNER JOIN dbo.Products AS product
            ON product.TenantId = lot.TenantId
           AND product.ProductId = lot.ProductId
        INNER JOIN dbo.UnitOfMeasure AS unit
            ON unit.UnitOfMeasureId = product.UnitOfMeasureId
        WHERE line.TenantId = @TenantId
          AND line.InventoryTransferId = @InventoryTransferId;

        SET @LineCount = @@ROWCOUNT;

        IF @LineCount = 0
        BEGIN
            IF @CurrentStatus = 'RECEIVED'
                THROW 51012, N'RECEIVED transfer has no lines; its history is inconsistent.', 1;

            THROW 51008, N'Transfer must contain at least one line.', 1;
        END;

        -- locks also protect the absence of movements before a new posting
        INSERT INTO @Movements
        (
            InventoryMovementId, TenantId, InventoryTransferLineId,
            InventoryLotId, Quantity, SourceLocationId,
            DestinationLocationId, CreatedAtUtc
        )
        SELECT
            movement.InventoryMovementId,
            movement.TenantId,
            movement.InventoryTransferLineId,
            movement.InventoryLotId,
            movement.Quantity,
            movement.SourceLocationId,
            movement.DestinationLocationId,
            movement.CreatedAtUtc
        FROM dbo.InventoryMovements AS movement WITH (UPDLOCK)
        INNER JOIN @Lines AS line
            ON line.InventoryTransferLineId = movement.InventoryTransferLineId
        WHERE movement.TenantId = @TenantId;

        IF @CurrentStatus = 'RECEIVED'
        BEGIN
            -- exactly one full, correctly routed movement for every line
            --      foreign keys prevent orphan movements or a different tenant/lot
            IF @CompletedAtUtc IS NULL OR EXISTS
            (
                SELECT line.InventoryTransferLineId
                FROM @Lines AS line
                LEFT JOIN @Movements AS movement
                    ON movement.InventoryTransferLineId = line.InventoryTransferLineId
                GROUP BY line.InventoryTransferLineId
                HAVING COUNT(movement.InventoryMovementId) <> 1
                    OR SUM(CASE
                        WHEN movement.TenantId = @TenantId
                         AND movement.InventoryLotId = line.InventoryLotId
                         AND movement.Quantity = line.QuantityRequested
                         AND movement.SourceLocationId = @SourceLocationId
                         AND movement.DestinationLocationId = @DestinationLocationId
                        THEN 1 ELSE 0
                    END) <> 1
            )
                THROW 51012, N'RECEIVED transfer has incomplete or inconsistent movement history.', 1;

            SET @IsReplay = 1;
        END
        ELSE
        BEGIN
            IF EXISTS (SELECT 1 FROM @Movements)
                THROW 51012, N'APPROVED transfer already has movements; investigate its history.', 1;

            SET @OperationAtUtc = SYSUTCDATETIME();
            SET @MinimumExpirationDate = DATEADD(DAY, 7, CONVERT(DATE, @OperationAtUtc));

            SELECT @TenantIsActive = tenant.IsActive
            FROM dbo.Tenants AS tenant
            WHERE tenant.TenantId = @TenantId;

            IF ISNULL(@TenantIsActive, 0) <> 1
                THROW 51004, N'Tenant must be active.', 1;

            IF @SourceLocationId = @DestinationLocationId
                THROW 51005, N'Source and destination locations must differ.', 1;

            SELECT
                @SourceLocationIsActive = sourceLocation.IsActive,
                @DestinationLocationIsActive = destinationLocation.IsActive,
                @SourceWarehouseIsActive = sourceWarehouse.IsActive,
                @DestinationWarehouseIsActive = destinationWarehouse.IsActive
            FROM dbo.WarehouseLocations AS sourceLocation
            INNER JOIN dbo.Warehouses AS sourceWarehouse
                ON sourceWarehouse.TenantId = sourceLocation.TenantId
               AND sourceWarehouse.WarehouseId = sourceLocation.WarehouseId
            INNER JOIN dbo.WarehouseLocations AS destinationLocation
                ON destinationLocation.TenantId = sourceLocation.TenantId
               AND destinationLocation.WarehouseLocationId = @DestinationLocationId
            INNER JOIN dbo.Warehouses AS destinationWarehouse
                ON destinationWarehouse.TenantId = destinationLocation.TenantId
               AND destinationWarehouse.WarehouseId = destinationLocation.WarehouseId
            WHERE sourceLocation.TenantId = @TenantId
              AND sourceLocation.WarehouseLocationId = @SourceLocationId;

            IF @SourceLocationIsActive IS NULL
                OR @DestinationLocationIsActive IS NULL
                OR @SourceWarehouseIsActive IS NULL
                OR @DestinationWarehouseIsActive IS NULL
                THROW 51005, N'Both locations and their warehouses must belong to this tenant.', 1;

            IF @SourceWarehouseIsActive <> 1 OR @DestinationWarehouseIsActive <> 1
                THROW 51006, N'Both warehouses must be active.', 1;

            IF @SourceLocationIsActive <> 1 OR @DestinationLocationIsActive <> 1
                THROW 51007, N'Both locations must be active.', 1;

            SELECT TOP (1) @InvalidLineId = line.InventoryTransferLineId
            FROM @Lines AS line
            WHERE line.ProductIsActive <> 1
            ORDER BY line.InventoryTransferLineId;

            IF @InvalidLineId IS NOT NULL
            BEGIN
                SET @ErrorMessage = CONCAT(N'Product must be active for transfer line ', @InvalidLineId, N'.');
                THROW 51009, @ErrorMessage, 1;
            END;

            SELECT TOP (1) @InvalidLineId = line.InventoryTransferLineId
            FROM @Lines AS line
            WHERE line.ExpirationDate < @MinimumExpirationDate
            ORDER BY line.InventoryTransferLineId;

            IF @InvalidLineId IS NOT NULL
            BEGIN
                SET @ErrorMessage = CONCAT(N'Lot must have at least seven UTC calendar days of shelf life for transfer line ', @InvalidLineId, N'.');
                THROW 51010, @ErrorMessage, 1;
            END;

            -- Reserve existing rows and missing keys at BOTH locations.
            -- The unique (TenantId, WarehouseLocationId, InventoryLotId) key and SERIALIZABLE range locks protect destination creation.
            INSERT INTO @Stock
            (
                WarehouseLocationId, InventoryLotId, InventoryBalanceId,
                QuantityOnHand, QuantityReserved
            )
            SELECT
                requested.WarehouseLocationId,
                requested.InventoryLotId,
                balance.InventoryBalanceId,
                balance.QuantityOnHand,
                balance.QuantityReserved
            FROM
            (
                SELECT @SourceLocationId AS WarehouseLocationId, line.InventoryLotId
                FROM @Lines AS line
                UNION ALL
                SELECT @DestinationLocationId, line.InventoryLotId
                FROM @Lines AS line
            ) AS requested
            LEFT JOIN dbo.InventoryBalances AS balance WITH (UPDLOCK)
                ON balance.TenantId = @TenantId
               AND balance.WarehouseLocationId = requested.WarehouseLocationId
               AND balance.InventoryLotId = requested.InventoryLotId;

            SELECT TOP (1) @InvalidLineId = line.InventoryTransferLineId
            FROM @Lines AS line
            INNER JOIN @Stock AS stock
                ON stock.InventoryLotId = line.InventoryLotId
               AND stock.WarehouseLocationId = @SourceLocationId
            WHERE stock.InventoryBalanceId IS NULL
            ORDER BY line.InventoryTransferLineId;

            IF @InvalidLineId IS NOT NULL
            BEGIN
                SET @ErrorMessage = CONCAT(N'No balance exists for the exact source location and lot of transfer line ', @InvalidLineId, N'.');
                THROW 51011, @ErrorMessage, 1;
            END;

            SELECT TOP (1) @InvalidLineId = line.InventoryTransferLineId
            FROM @Lines AS line
            INNER JOIN @Stock AS stock
                ON stock.InventoryLotId = line.InventoryLotId
               AND stock.WarehouseLocationId = @SourceLocationId
            WHERE stock.QuantityOnHand - stock.QuantityReserved < line.QuantityRequested
            ORDER BY line.InventoryTransferLineId;

            IF @InvalidLineId IS NOT NULL
            BEGIN
                SET @ErrorMessage = CONCAT(N'Insufficient unreserved source stock for transfer line ', @InvalidLineId, N'.');
                THROW 51013, @ErrorMessage, 1;
            END;

            UPDATE balance
            SET QuantityOnHand = balance.QuantityOnHand - line.QuantityRequested,
                UpdatedAtUtc = @OperationAtUtc
            FROM dbo.InventoryBalances AS balance
            INNER JOIN @Lines AS line
                ON line.InventoryLotId = balance.InventoryLotId
            WHERE balance.TenantId = @TenantId
              AND balance.WarehouseLocationId = @SourceLocationId
              AND balance.QuantityOnHand - balance.QuantityReserved >= line.QuantityRequested;

            IF @@ROWCOUNT <> @LineCount
                THROW 51014, N'Source posting did not update every transfer line.', 1;

            UPDATE balance
            SET QuantityOnHand = balance.QuantityOnHand + line.QuantityRequested,
                UpdatedAtUtc = @OperationAtUtc
            FROM dbo.InventoryBalances AS balance
            INNER JOIN @Lines AS line
                ON line.InventoryLotId = balance.InventoryLotId
            WHERE balance.TenantId = @TenantId
              AND balance.WarehouseLocationId = @DestinationLocationId;

            SET @DestinationUpdatedCount = @@ROWCOUNT;

            INSERT INTO dbo.InventoryBalances
            (
                TenantId, WarehouseLocationId, InventoryLotId,
                QuantityOnHand, QuantityReserved, CreatedAtUtc, UpdatedAtUtc
            )
            SELECT
                @TenantId, @DestinationLocationId, line.InventoryLotId,
                line.QuantityRequested, 0, @OperationAtUtc, NULL
            FROM @Lines AS line
            INNER JOIN @Stock AS stock
                ON stock.InventoryLotId = line.InventoryLotId
               AND stock.WarehouseLocationId = @DestinationLocationId
            WHERE stock.InventoryBalanceId IS NULL;

            IF @@ROWCOUNT + @DestinationUpdatedCount <> @LineCount
                THROW 51014, N'Destination posting did not apply every transfer line.', 1;

            INSERT INTO dbo.InventoryMovements
            (
                TenantId, InventoryTransferLineId, InventoryLotId,
                Quantity, SourceLocationId, DestinationLocationId, CreatedAtUtc
            )
            OUTPUT
                inserted.InventoryMovementId, inserted.TenantId,
                inserted.InventoryTransferLineId, inserted.InventoryLotId,
                inserted.Quantity, inserted.SourceLocationId,
                inserted.DestinationLocationId, inserted.CreatedAtUtc
            INTO @Movements
            (
                InventoryMovementId, TenantId, InventoryTransferLineId,
                InventoryLotId, Quantity, SourceLocationId,
                DestinationLocationId, CreatedAtUtc
            )
            SELECT
                @TenantId, line.InventoryTransferLineId, line.InventoryLotId,
                line.QuantityRequested, @SourceLocationId,
                @DestinationLocationId, @OperationAtUtc
            FROM @Lines AS line;

            IF @@ROWCOUNT <> @LineCount
                THROW 51014, N'Movement posting did not record every transfer line.', 1;

            UPDATE dbo.InventoryTransfers
            SET TransferStatusCode = 'RECEIVED',
                UpdatedAtUtc = @OperationAtUtc
            WHERE TenantId = @TenantId
              AND InventoryTransferId = @InventoryTransferId
              AND TransferStatusCode = 'APPROVED'
              AND RowVersion = @ExpectedRowVersion;

            IF @@ROWCOUNT <> 1
                THROW 51003, N'The transfer changed before completion.', 1;

            SELECT
                @CurrentStatus = transfer.TransferStatusCode,
                @CurrentRowVersion = transfer.RowVersion,
                @CompletedAtUtc = transfer.UpdatedAtUtc
            FROM dbo.InventoryTransfers AS transfer
            WHERE transfer.TenantId = @TenantId
              AND transfer.InventoryTransferId = @InventoryTransferId;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;

    -- captured facts are returned only after commit... with no live balance reads
    SELECT
        @InventoryTransferPublicId AS InventoryTransferPublicId,
        @CurrentStatus AS TransferStatusCode,
        @CompletedAtUtc AS CompletedAtUtc,
        @CurrentRowVersion AS RowVersion,
        @LineCount AS LineCount,
        @IsReplay AS IsReplay;

    SELECT
        line.InventoryTransferLineId,
        line.SKU,
        line.LotNumber,
        line.UnitOfMeasureCode,
        movement.Quantity AS QuantityMoved,
        movement.InventoryMovementId,
        movement.CreatedAtUtc AS MovementCreatedAtUtc
    FROM @Lines AS line
    INNER JOIN @Movements AS movement
        ON movement.InventoryTransferLineId = line.InventoryTransferLineId
    ORDER BY line.InventoryTransferLineId;
END;
GO
