
-- roll back if anything fails
SET XACT_ABORT ON; 

DECLARE @MedOneTenantId INT;
DECLARE @WarehouseAId INT;
DECLARE @WarehouseBId INT;

DECLARE @MainLocationId INT;
DECLARE @ReserveLocationId INT;

DECLARE @EachUnitOfMeasureId INT;

DECLARE @ProductId INT;
DECLARE @InventoryLotId INT;
DECLARE @InventoryTransferId INT;
DECLARE @InventoryTransferLineId INT;

DECLARE @InsertedTenants TABLE (
    TenantId INT,
    TenantName NVARCHAR(200)
);

DECLARE @InsertedWarehouses TABLE (
    WarehouseId INT,
    WarehouseName NVARCHAR(200)
);

DECLARE @InsertedWarehouseLocations TABLE (
	WarehouseLocationId INT,
	DisplayName NVARCHAR(100)
);

-- remaining parts are simple enough for a reusable table
DECLARE @InsertedId TABLE (
    Id INT
);

BEGIN TRANSACTION;

BEGIN TRY

	-- Insert tenant
    INSERT INTO dbo.Tenants (TenantName)
    OUTPUT
        INSERTED.TenantId,
        INSERTED.TenantName
    INTO @InsertedTenants (TenantId, TenantName)
    VALUES
    (N'Med One Medical Supplies');

    --extract variables
    SELECT @MedOneTenantId = TenantId
    FROM @InsertedTenants
    WHERE TenantName = N'Med One Medical Supplies'

    -- Insert Warehouses
    INSERT INTO dbo.Warehouses (
        TenantId,
        WarehouseCode,
        WarehouseName
    )
    OUTPUT
        INSERTED.WarehouseId,
        INSERTED.WarehouseName
    INTO @InsertedWarehouses (WarehouseId, WarehouseName)
    VALUES
        (@MedOneTenantId, N'P3-MedOne-Main', N'Med One Main Warehouse'),
        (@MedOneTenantId, N'P3-MedOne-Reserve', N'Med One Reserve Warehouse');

    --extract variables
    SELECT @WarehouseAId = WarehouseId
    FROM @InsertedWarehouses
    WHERE WarehouseName = N'Med One Main Warehouse'
    
    SELECT @WarehouseBId = WarehouseId
    FROM @InsertedWarehouses
    WHERE WarehouseName = N'Med One Reserve Warehouse'

    -- Insert warehouse addresses
	INSERT INTO dbo.WarehouseAddresses (
		TenantId,
		WarehouseId,
		AddressLine1,
		AddressLine2,
		City,
		StateCode,
		ZipCode
	)
	VALUES
	(@MedOneTenantId, @WarehouseAId, N'900 MedOne Avenue', N'Suite 900', N'Satsop', 'WA', '98583'),
	(@MedOneTenantId, @WarehouseBId, N'100 Taylor Road', NULL, N'Satsop', 'WA', '98583');

	-- Insert warehouse contacts
	INSERT INTO dbo.WarehouseContacts (
		TenantId,
		WarehouseId,
		ContactName,
		ContactRole,
		PrimaryPhoneNumber,
		PrimaryPhoneExtension,
		SecondaryPhoneNumber,
		SecondaryPhoneExtension,
		EmailAddress,
		IsActive
	)
	VALUES
	(@MedOneTenantId, @WarehouseAId, N'Saige Freya', N'Warehouse manager', '433-555-8759', '101', '312-555-0102', NULL, N's.freya@medone.example', 1),
	(@MedOneTenantId, @WarehouseAId, N'Alora Vance', N'Former warehouse manager', '433-555-9098', NULL, NULL, NULL, N'a.vance@medone.example', 0),
	(@MedOneTenantId, @WarehouseBId, N'Bowen Higgins', N'Reserve warehouse supervisor', '433-555-1028', '201', NULL, NULL, N'b.higgins@medone.example', 1),
	(@MedOneTenantId, @WarehouseBId, N'Michael Gentry', N'Reserve Warehouse manager', '433-555-3927', NULL, '433-555-2213', '301', N'm.gentry@medone.example', 1);

    -- Insert locations
    INSERT INTO dbo.WarehouseLocations(
		WarehouseId,
		TenantId,
		DisplayName,
		LocationCode
    )
    OUTPUT
        INSERTED.WarehouseLocationId,
        INSERTED.DisplayName
    INTO @InsertedWarehouseLocations (WarehouseLocationId, DisplayName)
    VALUES
    (@WarehouseAId, @MedOneTenantId, N'Aisle One', N'P3-Aisle-One'),
    (@WarehouseBId, @MedOneTenantId, N'Reserve Stock', N'P3-Stock');

    -- extract variables
    SELECT @MainLocationId = WarehouseLocationId
    FROM @InsertedWarehouseLocations
    WHERE DisplayName = N'Aisle One'
    
    SELECT @ReserveLocationId = WarehouseLocationId
    FROM @InsertedWarehouseLocations
    WHERE DisplayName = N'Reserve Stock'


    -- extract unit of measure ID
    SELECT @EachUnitOfMeasureId = UnitOfMeasureId
    FROM dbo.UnitOfMeasure
    WHERE UnitOfMeasureName = N'Each';


    -- Insert product
    INSERT INTO dbo.Products (
        TenantId,
        SKU,
        Name,
        Description,
        UnitOfMeasureId
    )
    OUTPUT INSERTED.ProductId
    INTO @InsertedId (Id)
    VALUES
    (@MedOneTenantId, N'P3-SUPPLY-BIN', N'Medical supply bin', N'One bin is one stocking unit.', @EachUnitOfMeasureId);

    SELECT @ProductId = Id
    FROM @InsertedId;

    DELETE FROM @InsertedId;

    -- Insert nonexpiring lot
    INSERT INTO dbo.InventoryLots (
        TenantId,
        ProductId,
        LotNumber,
        SupplierLotReference
    )
    OUTPUT INSERTED.InventoryLotId
    INTO @InsertedId (Id)
    VALUES
    (@MedOneTenantId, @ProductId, N'P3-BIN-A', N'P3-SUP-BIN-A');

    SELECT @InventoryLotId = Id
    FROM @InsertedId;

    DELETE FROM @InsertedId;

    
    -- NOTE: CompleteInventoryTransfer sproc treats a RECEIVED transfer’s UpdatedAtUtc as its completion timestamp
    --      seed that column, so replaying its completed transfer will pass the history check
    DECLARE @CompletedAtUtc DATETIME2(7) = SYSUTCDATETIME();

    -- Insert completed transfer
    INSERT INTO dbo.InventoryTransfers (
        TenantId,
        TransferStatusCode,
        SourceLocationId,
        DestinationLocationId,
        CreatedAtUtc,
        UpdatedAtUtc
    )
    OUTPUT INSERTED.InventoryTransferId
    INTO @InsertedId (Id)
    VALUES
    (@MedOneTenantId, 'RECEIVED', @MainLocationId, @ReserveLocationId, @CompletedAtUtc, @CompletedAtUtc);

    SELECT @InventoryTransferId = Id
    FROM @InsertedId;

    DELETE FROM @InsertedId;

    -- Insert a transfer line requesting 30 units
    INSERT INTO dbo.InventoryTransferLines (
        TenantId,
        InventoryTransferId,
        InventoryLotId,
        QuantityRequested,
        CreatedAtUtc
    )
    OUTPUT INSERTED.InventoryTransferLineId
    INTO @InsertedId (Id)
    VALUES
    (@MedOneTenantId, @InventoryTransferId, @InventoryLotId, 30.0000, @CompletedAtUtc);

    SELECT @InventoryTransferLineId = Id
    FROM @InsertedId;

    DELETE FROM @InsertedId;

    -- Record the completed movement of 30 units
    INSERT INTO dbo.InventoryMovements (
        TenantId,
        InventoryTransferLineId,
        InventoryLotId,
        Quantity,
        SourceLocationId,
        DestinationLocationId,
        CreatedAtUtc
    )
    VALUES
    (@MedOneTenantId, @InventoryTransferLineId, @InventoryLotId, 30.0000, @MainLocationId, @ReserveLocationId, @CompletedAtUtc);


    -- Insert the resulting balances:
    --      Opening stock: source 100, destination 0
    --      Completed move: 30 from source to destination
    --      Final stock: source 70, destination 30
    --      Reserved stock: 0 at both locations
    INSERT INTO dbo.InventoryBalances (
        TenantId,
        WarehouseLocationId,
        InventoryLotId,
        QuantityOnHand,
        QuantityReserved,
        CreatedAtUtc,
        UpdatedAtUtc
    )
    VALUES
    (@MedOneTenantId, @MainLocationId, @InventoryLotId, 70.0000, 0.0000, @CompletedAtUtc, @CompletedAtUtc),
    (@MedOneTenantId, @ReserveLocationId, @InventoryLotId, 30.0000, 0.0000, @CompletedAtUtc, NULL);

	COMMIT TRANSACTION;

END TRY
BEGIN CATCH
    -- undo everything
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END

    -- output error details
    PRINT 'Script failed! All changes have been rolled back.';
    SELECT 
        ERROR_NUMBER() AS ErrorNumber,
        ERROR_MESSAGE() AS ErrorMessage,
        ERROR_LINE() AS ErrorLine;

	THROW;
END CATCH;
GO
