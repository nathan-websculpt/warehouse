
-- roll back if anything fails
SET XACT_ABORT ON; 

DECLARE @CedarTenantId INT;
DECLARE @MapleTenantId INT;

DECLARE @EachUnitOfMeasureId INT;

DECLARE @CedarMainWarehouseId INT;
DECLARE @CedarReserveWarehouseId INT;
DECLARE @MapleMainWarehouseId INT;

DECLARE @PickAId INT;
DECLARE @PickBId INT;
DECLARE @ClosedLocationId INT;
DECLARE @ReserveLocationId INT;
DECLARE @MapleLocationId INT;

DECLARE @DogFoodProductId INT;
DECLARE @TreatProductId INT;
DECLARE @LitterProductId INT;
DECLARE @HarnessProductId INT;
DECLARE @ShampooProductId INT;
DECLARE @MapleDogFoodProductId INT;

DECLARE @Dog30LotId INT;
DECLARE @Dog60LotId INT;
DECLARE @ExpiredDogLotId INT;
DECLARE @ClosedDogLotId INT;
DECLARE @ClosedExpiredDogLotId INT;
DECLARE @TodayTreatLotId INT;
DECLARE @FutureTreatLotId INT;
DECLARE @LitterALotId INT;
DECLARE @LitterBLotId INT;
DECLARE @ShampooLotId INT;
DECLARE @MapleDogLotId INT;

DECLARE @SeedDate DATETIME2(7);

DECLARE @InsertedTenants TABLE (
	TenantId INT,
	TenantName NVARCHAR(200)
);

DECLARE @InsertedWarehouses TABLE (
	WarehouseId INT,
	WarehouseName NVARCHAR(200)
);

DECLARE @InsertedWarehouseLocations TABLE (
	WarehouseId INT,
	TenantId INT,
	WarehouseLocationId INT,
	DisplayName NVARCHAR(100)
);

DECLARE @InsertedProducts TABLE (
	ProductId INT,
	TenantId INT,
	SKU NVARCHAR(100),
	CreatedAtUtc DATETIME2(7)
);

DECLARE @InsertedLots TABLE (
	InventoryLotId INT,
	TenantId INT,
	ProductId INT,
	LotNumber NVARCHAR(100)
);

BEGIN TRANSACTION;

BEGIN TRY

	-- Insert tenants
	INSERT INTO dbo.Tenants (
		TenantName
	)
	OUTPUT
		INSERTED.TenantId,
		INSERTED.TenantName
	INTO @InsertedTenants (TenantId, TenantName)
	VALUES
	(N'Cedar Pet Supplies'),
	(N'Maple Pet Supplies');

	-- extract variables
	SELECT @CedarTenantId = TenantId
	FROM @InsertedTenants
	WHERE TenantName = N'Cedar Pet Supplies'

	SELECT @MapleTenantId = TenantId
	FROM @InsertedTenants
	WHERE TenantName = N'Maple Pet Supplies'

	-- Extract varialbe for 'Each' Unit of Measure Id
	SELECT @EachUnitOfMeasureId = UnitOfMeasureId
	FROM dbo.UnitOfMeasure
	Where UnitOfMeasureName = N'Each'

	-- Insert warehouses
	INSERT INTO dbo.Warehouses(
		TenantId,
		WarehouseCode,
		WarehouseName
	)
	OUTPUT
		INSERTED.WarehouseId,
		INSERTED.WarehouseName
	INTO @InsertedWarehouses (WarehouseId, WarehouseName)
	VALUES
	(@CedarTenantId, N'P2-CEDAR-MAIN', N'Cedar main warehouse'),
	(@CedarTenantId, N'P2-CEDAR-RESERVE', N'Cedar reserve warehouse'),
	(@MapleTenantId, N'P2-MAPLE-MAIN', N'Maple main warehouse')

	-- extract variables
	SELECT @CedarMainWarehouseId = WarehouseId
	FROM @InsertedWarehouses
	WHERE WarehouseName = N'Cedar main warehouse'
	
	SELECT @CedarReserveWarehouseId = WarehouseId
	FROM @InsertedWarehouses
	WHERE WarehouseName = N'Cedar reserve warehouse'
	
	SELECT @MapleMainWarehouseId = WarehouseId
	FROM @InsertedWarehouses
	WHERE WarehouseName = N'Maple main warehouse'

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
	(@CedarTenantId, @CedarMainWarehouseId, N'400 Cedar Avenue', N'Suite 100', N'Chicago', 'IL', '60607'),
	(@CedarTenantId, @CedarReserveWarehouseId, N'500 Reserve Road', NULL, N'Rockford', 'IL', '61101'),
	(@MapleTenantId, @MapleMainWarehouseId, N'600 Maple Drive', NULL, N'Madison', 'WI', '53703');

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
	(@CedarTenantId, @CedarMainWarehouseId, N'Taylor Reed', N'Warehouse manager', '312-555-0101', '101', '312-555-0102', NULL, N'taylor.reed@cedar.example', 1),
	(@CedarTenantId, @CedarMainWarehouseId, N'Sam Quinn', N'Former warehouse manager', '312-555-0103', NULL, NULL, NULL, N'sam.quinn@cedar.example', 0),
	(@CedarTenantId, @CedarReserveWarehouseId, N'Avery Grant', N'Reserve warehouse supervisor', '815-555-0101', '201', NULL, NULL, N'avery.grant@cedar.example', 1),
	(@MapleTenantId, @MapleMainWarehouseId, N'Riley Morgan', N'Warehouse manager', '608-555-0101', NULL, '608-555-0102', '301', N'riley.morgan@maple.example', 1);

	-- insert locations
	INSERT INTO dbo.WarehouseLocations(
		WarehouseId,
		TenantId,
		DisplayName,
		LocationCode,
		IsActive
	)
	OUTPUT
		INSERTED.WarehouseId,
		INSERTED.TenantId,
		INSERTED.WarehouseLocationId,
		INSERTED.DisplayName
	INTO @InsertedWarehouseLocations (WarehouseId, TenantId, WarehouseLocationId, DisplayName)
	VALUES
	(@CedarMainWarehouseId, @CedarTenantId, N'Picking aisle A', N'P2-PICK-A', 1),
	(@CedarMainWarehouseId, @CedarTenantId, N'Picking aisle B', N'P2-PICK-B', 1),
	(@CedarMainWarehouseId, @CedarTenantId, N'Closed storage location', N'P2-CLOSED', 0),
	(@CedarReserveWarehouseId, @CedarTenantId, N'Reserve warehouse stock', N'P2-STOCK', 1),
	(@MapleMainWarehouseId, @MapleTenantId, N'Maple warehouse stock', N'P2-STOCK', 1);

	-- extract variables
	SELECT @PickAId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Picking aisle A'
		AND WarehouseId = @CedarMainWarehouseId
		AND TenantId = @CedarTenantId
			
	SELECT @PickBId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Picking aisle B'
		AND WarehouseId = @CedarMainWarehouseId
		AND TenantId = @CedarTenantId

	SELECT @ClosedLocationId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Closed storage location'
		AND WarehouseId = @CedarMainWarehouseId
		AND TenantId = @CedarTenantId
			
	SELECT @ReserveLocationId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Reserve warehouse stock'
		AND WarehouseId = @CedarReserveWarehouseId
		AND TenantId = @CedarTenantId
			
	SELECT @MapleLocationId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Maple warehouse stock'
		AND WarehouseId = @MapleMainWarehouseId
		AND TenantId = @MapleTenantId

	-- insert products
	INSERT INTO dbo.Products (
		TenantId,
		SKU,
		Name,
		Description,
		UnitOfMeasureId,
		IsActive
	)
	OUTPUT
		INSERTED.ProductId,
		INSERTED.TenantId,
		INSERTED.SKU,
		INSERTED.CreatedAtUtc
	INTO @InsertedProducts (ProductId, TenantId, SKU, CreatedAtUtc)
	VALUES
	(@CedarTenantId, N'P2-DOG-FOOD', N'Dry dog food bag', N'One sealed bag is one stocking piece.', @EachUnitOfMeasureId, 1),
	(@CedarTenantId, N'P2-DOG-TREATS', N'Dog treat packet', N'One sealed packet is one stocking piece.', @EachUnitOfMeasureId, 1),
	(@CedarTenantId, N'P2-CAT-LITTER', N'Cat litter bag', N'Nonexpiring stock; one bag is one piece.', @EachUnitOfMeasureId, 1),
	(@CedarTenantId, N'P2-DOG-HARNESS', N'Dog harness', N'Active catalog product with no lots or stock yet.', @EachUnitOfMeasureId, 1),
	(@CedarTenantId, N'P2-OLD-SHAMPOO', N'Discontinued pet shampoo', N'Inactive product retained with its existing stock.', @EachUnitOfMeasureId, 0),
	(@MapleTenantId, N'P2-DOG-FOOD', N'Dry dog food bag', N'Independent stock owned by Maple.', @EachUnitOfMeasureId, 1);

	-- extract variables
	SELECT @DogFoodProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @CedarTenantId
		AND SKU = N'P2-DOG-FOOD'
	
	SELECT @TreatProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @CedarTenantId
		AND SKU = N'P2-DOG-TREATS'
	
	SELECT @LitterProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @CedarTenantId
		AND SKU = N'P2-CAT-LITTER'
	
	SELECT @HarnessProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @CedarTenantId
		AND SKU = N'P2-DOG-HARNESS'
	
	SELECT @ShampooProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @CedarTenantId
		AND SKU = N'P2-OLD-SHAMPOO'
	
	SELECT @MapleDogFoodProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @MapleTenantId
		AND SKU = N'P2-DOG-FOOD'

	-- extract a single Seed Date for simplicity
	SELECT @SeedDate = CreatedAtUtc
	FROM @InsertedProducts
	WHERE TenantId = @MapleTenantId
		AND SKU = N'P2-DOG-FOOD'

	-- insert Inventory Lots
	INSERT INTO dbo.InventoryLots (
		TenantId,
		ProductId,
		LotNumber,
		ManufactureDate,
		ExpirationDate,
		SupplierLotReference
	)
	OUTPUT
		INSERTED.InventoryLotId,
		INSERTED.TenantId,
		INSERTED.ProductId,
		INSERTED.LotNumber
	INTO @InsertedLots (InventoryLotId, TenantId, ProductId, LotNumber)
	VALUES
	(@CedarTenantId, @DogFoodProductId, N'P2-DOG-30', DATEADD(DAY, -60, @SeedDate), DATEADD(DAY, 30, @SeedDate), N'P2-SUP-DOG-30'),
	(@CedarTenantId, @DogFoodProductId, N'P2-DOG-60', DATEADD(DAY, -30, @SeedDate), DATEADD(DAY, 60, @SeedDate), N'P2-SUP-DOG-60'),
	(@CedarTenantId, @DogFoodProductId, N'P2-DOG-EXPIRED', DATEADD(DAY, -120, @SeedDate), DATEADD(DAY, -1, @SeedDate), N'P2-SUP-DOG-OLD'),
	(@CedarTenantId, @DogFoodProductId, N'P2-DOG-CLOSED', DATEADD(DAY, -30, @SeedDate), DATEADD(DAY, 45, @SeedDate), N'P2-SUP-DOG-CLOSED'),
	(@CedarTenantId, @DogFoodProductId, N'P2-DOG-CLOSED-EXPIRED', DATEADD(DAY, -120, @SeedDate), DATEADD(DAY, -1, @SeedDate), N'P2-SUP-DOG-BOTH'),
	(@CedarTenantId, @TreatProductId, N'P2-TREAT-TODAY', DATEADD(DAY, -60, @SeedDate), @SeedDate, N'P2-SUP-TREAT-TODAY'),
	(@CedarTenantId, @TreatProductId, N'P2-TREAT-FUTURE', DATEADD(DAY, -30, @SeedDate), DATEADD(DAY, 90, @SeedDate), N'P2-SUP-TREAT-FUTURE'),
	(@CedarTenantId, @LitterProductId, N'P2-LITTER-A', NULL, NULL, N'P2-SUP-LITTER-A'),
	(@CedarTenantId, @LitterProductId, N'P2-LITTER-B', NULL, NULL, N'P2-SUP-LITTER-B'),
	(@CedarTenantId, @ShampooProductId, N'P2-SHAMPOO-A', NULL, NULL, N'P2-SUP-SHAMPOO-A'),
	(@MapleTenantId, @MapleDogFoodProductId, N'P2-DOG-30', DATEADD(DAY, -60, @SeedDate), DATEADD(DAY, 30, @SeedDate), N'P2-MAPLE-DOG-30');

	-- extract variables
	SELECT @Dog30LotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @CedarTenantId
		AND ProductId = @DogFoodProductId
		AND LotNumber = N'P2-DOG-30';

	SELECT @Dog60LotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @CedarTenantId
		AND ProductId = @DogFoodProductId
		AND LotNumber = N'P2-DOG-60';

	SELECT @ExpiredDogLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @CedarTenantId
		AND ProductId = @DogFoodProductId
		AND LotNumber = N'P2-DOG-EXPIRED';

	SELECT @ClosedDogLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @CedarTenantId
		AND ProductId = @DogFoodProductId
		AND LotNumber = N'P2-DOG-CLOSED';

	SELECT @ClosedExpiredDogLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @CedarTenantId
		AND ProductId = @DogFoodProductId
		AND LotNumber = N'P2-DOG-CLOSED-EXPIRED';

	SELECT @TodayTreatLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @CedarTenantId
		AND ProductId = @TreatProductId
		AND LotNumber = N'P2-TREAT-TODAY';

	SELECT @FutureTreatLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @CedarTenantId
		AND ProductId = @TreatProductId
		AND LotNumber = N'P2-TREAT-FUTURE';

	SELECT @LitterALotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @CedarTenantId
		AND ProductId = @LitterProductId
		AND LotNumber = N'P2-LITTER-A';

	SELECT @LitterBLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @CedarTenantId
		AND ProductId = @LitterProductId
		AND LotNumber = N'P2-LITTER-B';

	SELECT @ShampooLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @CedarTenantId
		AND ProductId = @ShampooProductId
		AND LotNumber = N'P2-SHAMPOO-A';

	SELECT @MapleDogLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @MapleTenantId
		AND ProductId = @MapleDogFoodProductId
		AND LotNumber = N'P2-DOG-30';

	-- Insert inventory balances

	INSERT INTO dbo.InventoryBalances (
		TenantId,
		WarehouseLocationId,
		InventoryLotId,
		QuantityOnHand,
		QuantityReserved
	)
	VALUES
    (@CedarTenantId, @PickAId, @Dog30LotId, 100.0000, 20.0000),
    (@CedarTenantId, @PickBId, @Dog30LotId, 25.0000, 5.0000),
    (@CedarTenantId, @PickBId, @Dog60LotId, 40.0000, 10.0000),
    (@CedarTenantId, @PickAId, @ExpiredDogLotId, 80.0000, 0.0000),
    (@CedarTenantId, @ClosedLocationId, @ClosedDogLotId, 50.0000, 5.0000),
    (@CedarTenantId, @ClosedLocationId, @ClosedExpiredDogLotId, 12.0000, 2.0000),
    (@CedarTenantId, @ReserveLocationId, @Dog30LotId, 200.0000, 0.0000),
    (@CedarTenantId, @PickAId, @TodayTreatLotId, 18.0000, 3.0000),
    (@CedarTenantId, @PickBId, @FutureTreatLotId, 12.0000, 12.0000),
    (@CedarTenantId, @PickAId, @LitterALotId, 50.0000, 50.0000),
    (@CedarTenantId, @PickBId, @LitterBLotId, 0.0000, 0.0000),
    (@CedarTenantId, @PickAId, @ShampooLotId, 8.0000, 0.0000),
    (@MapleTenantId, @MapleLocationId, @MapleDogLotId, 999.0000, 0.0000);

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
