
-- roll back if anything fails
SET XACT_ABORT ON; 

DECLARE @GreenleafGardenTenantId INT;
DECLARE @FertilizersTenantId INT;

DECLARE @EachUnitOfMeasureId INT;
DECLARE @KilogramUnitOfMeasureId INT;
DECLARE @GallonUnitOfMeasureId INT;

DECLARE @GreenleafGardenWarehouseId INT;
DECLARE @FertilizersWarehouseId INT;
DECLARE @GreenleafNurseryWarehouseId INT;

DECLARE @MainStockId INT;
DECLARE @OverflowStockId INT;
DECLARE @NurseryReceivingId INT;
DECLARE @NurseryReplenishmentId INT;
DECLARE @ClosedStorageId INT;
DECLARE @FertStockId INT;
DECLARE @FertShippingId INT;

DECLARE @TomatoSeedProductId INT;
DECLARE @BasilSeedProductId INT;
DECLARE @LiquidFeedProductId INT;
DECLARE @GranularFertProductId INT;
DECLARE @SprayerProductId INT;
DECLARE @TomatoSeedFertilizersRUsProductId INT;

DECLARE @TomatoALotId INT;
DECLARE @TomatoBLotId INT;
DECLARE @TomatoExpiredLotId INT;
DECLARE @TomatoOffsiteLotId INT;
DECLARE @BasilALotId INT;
DECLARE @FeedALotId INT;
DECLARE @FeedShortLotId INT;
DECLARE @FertALotId INT;
DECLARE @SprayerALotId INT;
DECLARE @FertilizersRUsTomatoALotId INT;

DECLARE @GSourceId INT;
DECLARE @GOverflowId INT;
DECLARE @FSourceId INT;
DECLARE @GDestId INT;
DECLARE @GDest2Id INT;
DECLARE @GInactiveId INT;
DECLARE @FDestId INT;

DECLARE @Transfer1Id INT;
DECLARE @Transfer2Id INT;
DECLARE @Transfer3Id INT;
DECLARE @Transfer4Id INT;
DECLARE @Transfer5Id INT;
DECLARE @Transfer6Id INT;
DECLARE @Transfer7Id INT;
DECLARE @Transfer8Id INT;
DECLARE @Transfer9Id INT;
DECLARE @Transfer10Id INT;
DECLARE @Transfer11Id INT;
DECLARE @Transfer12Id INT;
DECLARE @Transfer13Id INT;
DECLARE @Transfer14Id INT;
DECLARE @Transfer15Id INT;
DECLARE @Transfer1PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer2PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer3PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer4PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer5PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer6PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer7PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer8PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer9PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer10PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer11PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer12PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer13PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer14PublicId UNIQUEIDENTIFIER = NEWID();
DECLARE @Transfer15PublicId UNIQUEIDENTIFIER = NEWID();

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

DECLARE @InsertedTransfers TABLE (
    InventoryTransferId INT,
    InventoryTransferPublicId UNIQUEIDENTIFIER
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
	(N'Greenleaf Garden Supplies'),
	(N'Fertilizers R Us');

	-- extract variables
	SELECT @GreenleafGardenTenantId = TenantId
	FROM @InsertedTenants
	WHERE TenantName = N'Greenleaf Garden Supplies'

	SELECT @FertilizersTenantId = TenantId
	FROM @InsertedTenants
	WHERE TenantName = N'Fertilizers R Us'

	-- Extract variables for Unit of Measure IDs
	SELECT @EachUnitOfMeasureId = UnitOfMeasureId
	FROM dbo.UnitOfMeasure
	Where UnitOfMeasureName = N'Each'
	SELECT @KilogramUnitOfMeasureId = UnitOfMeasureId
	FROM dbo.UnitOfMeasure
	Where UnitOfMeasureName = N'Kilogram'
	SELECT @GallonUnitOfMeasureId = UnitOfMeasureId
	FROM dbo.UnitOfMeasure
	Where UnitOfMeasureName = N'Gallon'

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
	(@GreenleafGardenTenantId, N'P1-GREENLEAF-GARDEN', N'Greenleaf Main Warehouse'),
	(@GreenleafGardenTenantId, N'P1-GREENLEAF-NURSERY', N'Greenleaf Nursery and Retail'),
	(@FertilizersTenantId, N'P1-FERT-MAIN', N'Fertilizers R Us Main Warehouse')

	-- extract variables
	SELECT @GreenleafGardenWarehouseId = WarehouseId
	FROM @InsertedWarehouses
	WHERE WarehouseName = N'Greenleaf Main Warehouse'
	
	SELECT @GreenleafNurseryWarehouseId = WarehouseId
	FROM @InsertedWarehouses
	WHERE WarehouseName = N'Greenleaf Nursery and Retail'
	
	SELECT @FertilizersWarehouseId = WarehouseId
	FROM @InsertedWarehouses
	WHERE WarehouseName = N'Fertilizers R Us Main Warehouse'

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
	(@GreenleafGardenWarehouseId, @GreenleafGardenTenantId, N'Main warehouse stock', N'STOCK-01', 1),
	(@GreenleafGardenWarehouseId, @GreenleafGardenTenantId, N'Main warehouse overflow', N'OVERFLOW-01', 1),
	(@GreenleafNurseryWarehouseId, @GreenleafGardenTenantId, N'Nursery receiving', N'RECEIVING-01', 1),
	(@GreenleafNurseryWarehouseId, @GreenleafGardenTenantId, N'Nursery replenishment bench', N'BENCH-01', 1),
	(@GreenleafGardenWarehouseId, @GreenleafGardenTenantId, N'Closed storage area', N'CLOSED-01', 0),
	(@FertilizersWarehouseId, @FertilizersTenantId, N'Fertilizers R Us stock', N'STOCK-01', 1),
	(@FertilizersWarehouseId, @FertilizersTenantId, N'Fertilizers R Us shipping', N'SHIPPING-01', 1);

	-- extract variables
	SELECT @MainStockId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Main warehouse stock'
		AND WarehouseId = @GreenleafGardenWarehouseId
		AND TenantId = @GreenleafGardenTenantId
			
	SELECT @OverflowStockId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Main warehouse overflow'
		AND WarehouseId = @GreenleafGardenWarehouseId
		AND TenantId = @GreenleafGardenTenantId

	SELECT @NurseryReceivingId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Nursery receiving'
		AND WarehouseId = @GreenleafNurseryWarehouseId
		AND TenantId = @GreenleafGardenTenantId
			
	SELECT @NurseryReplenishmentId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Nursery replenishment bench'
		AND WarehouseId = @GreenleafNurseryWarehouseId
		AND TenantId = @GreenleafGardenTenantId
			
	SELECT @ClosedStorageId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Closed storage area'
		AND WarehouseId = @GreenleafGardenWarehouseId
		AND TenantId = @GreenleafGardenTenantId
			
	SELECT @FertStockId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Fertilizers R Us stock'
		AND WarehouseId = @FertilizersWarehouseId
		AND TenantId = @FertilizersTenantId
			
	SELECT @FertShippingId = WarehouseLocationId
	FROM @InsertedWarehouseLocations
	WHERE DisplayName = N'Fertilizers R Us shipping'
		AND WarehouseId = @FertilizersWarehouseId
		AND TenantId = @FertilizersTenantId

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
	(@GreenleafGardenTenantId, N'P1-TOMATO-SEED', N'Tomato seed packet', N'One packet is one stocking unit.', @EachUnitOfMeasureId, 1),
	(@GreenleafGardenTenantId, N'P1-BASIL-SEED', N'Basil seed packet', N'One packet is one stocking unit.', @EachUnitOfMeasureId, 1),
	(@GreenleafGardenTenantId, N'P1-LIQUID-FEED', N'Liquid plant feed', N'Stocked and transferred in gallons.', @GallonUnitOfMeasureId, 1),
	(@GreenleafGardenTenantId, N'P1-GRANULAR-FERT', N'Granular fertilizer', N'Stocked and transferred in kilograms.', @KilogramUnitOfMeasureId, 1),
	(@GreenleafGardenTenantId, N'P1-OLD-SPRAYER', N'Discontinued garden sprayer', N'Inactive product retained for history.', @EachUnitOfMeasureId, 0),
	(@FertilizersTenantId, N'P1-TOMATO-SEED', N'Tomato seed packet', N'Independent stock owned by Fertilizers R Us.', @EachUnitOfMeasureId, 1);

	-- extract variables
	SELECT @TomatoSeedProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @GreenleafGardenTenantId
		AND SKU = N'P1-TOMATO-SEED'
	
	SELECT @BasilSeedProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @GreenleafGardenTenantId
		AND SKU = N'P1-BASIL-SEED'
	
	SELECT @LiquidFeedProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @GreenleafGardenTenantId
		AND SKU = N'P1-LIQUID-FEED'
	
	SELECT @GranularFertProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @GreenleafGardenTenantId
		AND SKU = N'P1-GRANULAR-FERT'
	
	SELECT @SprayerProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @GreenleafGardenTenantId
		AND SKU = N'P1-OLD-SPRAYER'
	
	SELECT @TomatoSeedFertilizersRUsProductId = ProductId
	FROM @InsertedProducts
	WHERE TenantId = @FertilizersTenantId
		AND SKU = N'P1-TOMATO-SEED'

	-- extract a single Seed Date for simplicity
	SELECT @SeedDate = CreatedAtUtc
	FROM @InsertedProducts
	WHERE TenantId = @GreenleafGardenTenantId
		AND SKU = N'P1-TOMATO-SEED'

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
	(@GreenleafGardenTenantId, @TomatoSeedProductId, N'P1-TOMATO-A', DATEADD(DAY, -120, @SeedDate), DATEADD(DAY, 90, @SeedDate), N'SUP-TOM-A'),
	(@GreenleafGardenTenantId, @TomatoSeedProductId, N'P1-TOMATO-B', DATEADD(DAY, -90, @SeedDate), DATEADD(DAY, 180, @SeedDate), N'SUP-TOM-B'),
	(@GreenleafGardenTenantId, @TomatoSeedProductId, N'P1-TOMATO-EXPIRED', DATEADD(DAY, -180, @SeedDate), DATEADD(DAY, -1, @SeedDate), N'SUP-TOM-OLD'),
	(@GreenleafGardenTenantId, @TomatoSeedProductId, N'P1-TOMATO-OFFSITE', DATEADD(DAY, -30, @SeedDate), DATEADD(DAY, 240, @SeedDate), N'SUP-TOM-OFF'),
	(@GreenleafGardenTenantId, @BasilSeedProductId, N'P1-BASIL-A', DATEADD(DAY, -100, @SeedDate), DATEADD(DAY, 60, @SeedDate), N'SUP-BAS-A'),
	(@GreenleafGardenTenantId, @LiquidFeedProductId, N'P1-FEED-A', DATEADD(DAY, -60, @SeedDate), DATEADD(DAY, 120, @SeedDate), N'SUP-FEED-A'),
	(@GreenleafGardenTenantId, @LiquidFeedProductId, N'P1-FEED-SHORT', DATEADD(DAY, -60, @SeedDate), DATEADD(DAY, 3, @SeedDate), N'SUP-FEED-SHORT'),
	(@GreenleafGardenTenantId, @GranularFertProductId, N'P1-FERT-A', DATEADD(DAY, -30, @SeedDate), NULL, N'SUP-FERT-A'),
	(@GreenleafGardenTenantId, @SprayerProductId, N'P1-SPRAYER-A', NULL, NULL, N'SUP-SPRAYER-A'),
	(@FertilizersTenantId, @TomatoSeedFertilizersRUsProductId, N'P1-TOMATO-A', DATEADD(DAY, -100, @SeedDate), DATEADD(DAY, 90, @SeedDate), N'OTHER-SUP-TOM-A');
	--
	-- extract variables
	SELECT @TomatoALotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @GreenleafGardenTenantId
		AND ProductId = @TomatoSeedProductId
		AND LotNumber = N'P1-TOMATO-A';

	SELECT @TomatoBLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @GreenleafGardenTenantId
		AND ProductId = @TomatoSeedProductId
		AND LotNumber = N'P1-TOMATO-B';

	SELECT @TomatoExpiredLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @GreenleafGardenTenantId
		AND ProductId = @TomatoSeedProductId
		AND LotNumber = N'P1-TOMATO-EXPIRED';

	SELECT @TomatoOffsiteLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @GreenleafGardenTenantId
		AND ProductId = @TomatoSeedProductId
		AND LotNumber = N'P1-TOMATO-OFFSITE';

	SELECT @BasilALotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @GreenleafGardenTenantId
		AND ProductId = @BasilSeedProductId
		AND LotNumber = N'P1-BASIL-A';

	SELECT @FeedALotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @GreenleafGardenTenantId
		AND ProductId = @LiquidFeedProductId
		AND LotNumber = N'P1-FEED-A';

	SELECT @FeedShortLotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @GreenleafGardenTenantId
		AND ProductId = @LiquidFeedProductId
		AND LotNumber = N'P1-FEED-SHORT';

	SELECT @FertALotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @GreenleafGardenTenantId
		AND ProductId = @GranularFertProductId
		AND LotNumber = N'P1-FERT-A';

	SELECT @SprayerALotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @GreenleafGardenTenantId
		AND ProductId = @SprayerProductId
		AND LotNumber = N'P1-SPRAYER-A';

	SELECT @FertilizersRUsTomatoALotId = InventoryLotId
	FROM @InsertedLots
	WHERE TenantId = @FertilizersTenantId
		AND ProductId = @TomatoSeedFertilizersRUsProductId
		AND LotNumber = N'P1-TOMATO-A';
		

	-- Insert inventory balances

	INSERT INTO dbo.InventoryBalances (
		TenantId,
		WarehouseLocationId,
		InventoryLotId,
		QuantityOnHand,
		QuantityReserved
	)
	VALUES
    (@GreenleafGardenTenantId, @MainStockId, @TomatoALotId, 100.0000, 20.0000),
	(@GreenleafGardenTenantId, @MainStockId, @TomatoBLotId, 50.0000, 10.0000),
	(@GreenleafGardenTenantId, @MainStockId, @TomatoExpiredLotId, 500.0000, 0.0000),
	(@GreenleafGardenTenantId, @MainStockId, @BasilALotId, 60.0000, 0.0000),
	(@GreenleafGardenTenantId, @MainStockId, @FeedALotId, 40.5000, 5.2500),
	(@GreenleafGardenTenantId, @MainStockId, @FeedShortLotId, 25.0000, 0.0000),
	(@GreenleafGardenTenantId, @MainStockId, @FertALotId, 120.7500, 20.2500),
	(@GreenleafGardenTenantId, @MainStockId, @SprayerALotId, 10.0000, 0.0000),
	(@GreenleafGardenTenantId, @OverflowStockId, @TomatoBLotId, 30.0000, 5.0000),
	(@GreenleafGardenTenantId, @OverflowStockId, @TomatoOffsiteLotId, 15.0000, 0.0000),
	(@GreenleafGardenTenantId, @OverflowStockId, @FertALotId, 30.0000, 0.0000),
	(@GreenleafGardenTenantId, @NurseryReceivingId, @TomatoALotId, 10.0000, 2.0000),
	(@GreenleafGardenTenantId, @NurseryReceivingId, @FeedALotId, 4.2500, 1.0000),
	(@FertilizersTenantId, @FertStockId, @FertilizersRUsTomatoALotId, 5000.0000, 0.0000);

	-- extract source/destination locations
	SELECT @GSourceId = WarehouseLocationId
	FROM dbo.WarehouseLocations
	WHERE TenantId = @GreenleafGardenTenantId AND WarehouseId = @GreenleafGardenWarehouseId AND LocationCode = N'STOCK-01'

	SELECT @GOverflowId = WarehouseLocationId
	FROM dbo.WarehouseLocations
	WHERE TenantId = @GreenleafGardenTenantId AND WarehouseId = @GreenleafGardenWarehouseId AND LocationCode = N'OVERFLOW-01'

	SELECT @FSourceId = WarehouseLocationId
	FROM dbo.WarehouseLocations
	WHERE TenantId = @FertilizersTenantId AND WarehouseId = @FertilizersWarehouseId AND LocationCode = N'STOCK-01'

	SELECT @GDestId = WarehouseLocationId
	FROM dbo.WarehouseLocations
	WHERE TenantId = @GreenleafGardenTenantId AND WarehouseId = @GreenleafNurseryWarehouseId AND LocationCode = N'RECEIVING-01'

	SELECT @GDest2Id = WarehouseLocationId
	FROM dbo.WarehouseLocations
	WHERE TenantId = @GreenleafGardenTenantId AND WarehouseId = @GreenleafNurseryWarehouseId AND LocationCode = N'BENCH-01'

	SELECT @GInactiveId = WarehouseLocationId
	FROM dbo.WarehouseLocations
	WHERE TenantId = @GreenleafGardenTenantId AND WarehouseId = @GreenleafGardenWarehouseId AND LocationCode = N'CLOSED-01'

	SELECT @FDestId = WarehouseLocationId
	FROM dbo.WarehouseLocations
	WHERE TenantId = @FertilizersTenantId AND WarehouseId = @FertilizersWarehouseId AND LocationCode = N'SHIPPING-01'

	-- Insert inventory transfers
	INSERT INTO dbo.InventoryTransfers (
		InventoryTransferPublicId,
		TenantId,
		TransferStatusCode,
		SourceLocationId,
		DestinationLocationId
	)
	OUTPUT
		INSERTED.InventoryTransferId,
		INSERTED.InventoryTransferPublicId
	INTO @InsertedTransfers (InventoryTransferId, InventoryTransferPublicId)
	VALUES
	(@Transfer1PublicId,  @GreenleafGardenTenantId, 'APPROVED', @GSourceId,   @GDestId),
	(@Transfer2PublicId,  @GreenleafGardenTenantId, 'APPROVED', @GSourceId,   @GDestId),
	(@Transfer3PublicId,  @GreenleafGardenTenantId, 'APPROVED', @GSourceId,   @GDestId),
	(@Transfer4PublicId,  @GreenleafGardenTenantId, 'APPROVED', @GSourceId,   @GDestId),
	(@Transfer5PublicId,  @GreenleafGardenTenantId, 'APPROVED', @GSourceId,   @GDestId),
	(@Transfer6PublicId,  @GreenleafGardenTenantId, 'APPROVED', @GSourceId,   @GDestId),
	(@Transfer7PublicId,  @GreenleafGardenTenantId, 'APPROVED', @GOverflowId, @GDestId),
	(@Transfer8PublicId,  @GreenleafGardenTenantId, 'APPROVED', @GOverflowId, @GDest2Id),
	(@Transfer9PublicId,  @GreenleafGardenTenantId, 'APPROVED', @GSourceId,   @GDest2Id),
	(@Transfer10PublicId, @GreenleafGardenTenantId, 'APPROVED', @GOverflowId, @GDest2Id),
	(@Transfer11PublicId, @GreenleafGardenTenantId, 'DRAFT',    @GSourceId,   @GDestId),
	(@Transfer12PublicId, @GreenleafGardenTenantId, 'APPROVED', @GSourceId,   @GInactiveId),
	(@Transfer13PublicId, @GreenleafGardenTenantId, 'APPROVED', @GSourceId,   @GDestId),
	(@Transfer14PublicId, @FertilizersTenantId,     'APPROVED', @FSourceId,   @FDestId),
	(@Transfer15PublicId, @GreenleafGardenTenantId, 'APPROVED', @GSourceId,   @GDestId);

	-- extract
	SELECT @Transfer1Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer1PublicId;

	SELECT @Transfer2Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer2PublicId;

	SELECT @Transfer3Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer3PublicId;

	SELECT @Transfer4Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer4PublicId;

	SELECT @Transfer5Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer5PublicId;

	SELECT @Transfer6Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer6PublicId;

	SELECT @Transfer7Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer7PublicId;

	SELECT @Transfer8Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer8PublicId;

	SELECT @Transfer9Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer9PublicId;

	SELECT @Transfer10Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer10PublicId;

	SELECT @Transfer11Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer11PublicId;

	SELECT @Transfer12Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer12PublicId;

	SELECT @Transfer13Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer13PublicId;

	SELECT @Transfer14Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer14PublicId;

	SELECT @Transfer15Id = InventoryTransferId
	FROM @InsertedTransfers
	WHERE InventoryTransferPublicId = @Transfer15PublicId;

	-- Insert Inventory Transfer Lines
	INSERT INTO dbo.InventoryTransferLines (
		TenantId,
		InventoryTransferId,
		InventoryLotId,
		QuantityRequested
	)
	VALUES
	(@GreenleafGardenTenantId, @Transfer1Id, @TomatoALotId, 30.0000),
	(@GreenleafGardenTenantId, @Transfer1Id, @TomatoBLotId, 20.0000),
	(@GreenleafGardenTenantId, @Transfer1Id, @BasilALotId, 15.0000),
	(@GreenleafGardenTenantId, @Transfer1Id, @FeedALotId, 12.7500),
	(@GreenleafGardenTenantId, @Transfer1Id, @FertALotId, 25.5000),
	(@GreenleafGardenTenantId, @Transfer2Id, @BasilALotId, 5.0000),
	(@GreenleafGardenTenantId, @Transfer2Id, @TomatoBLotId, 45.0000),
	(@GreenleafGardenTenantId, @Transfer3Id, @TomatoExpiredLotId, 5.0000),
	(@GreenleafGardenTenantId, @Transfer4Id, @FeedShortLotId, 5.0000),
	(@GreenleafGardenTenantId, @Transfer5Id, @SprayerALotId, 1.0000),
	(@GreenleafGardenTenantId, @Transfer6Id, @TomatoOffsiteLotId, 1.0000),
	(@GreenleafGardenTenantId, @Transfer7Id, @TomatoBLotId, 20.0000),
	(@GreenleafGardenTenantId, @Transfer8Id, @TomatoBLotId, 20.0000),
	(@GreenleafGardenTenantId, @Transfer9Id, @FertALotId, 10.0000),
	(@GreenleafGardenTenantId, @Transfer10Id,@FertALotId, 10.0000),
	(@GreenleafGardenTenantId, @Transfer11Id,@BasilALotId, 5.0000),
	(@GreenleafGardenTenantId, @Transfer12Id,@TomatoALotId, 5.0000),
	(@FertilizersTenantId, @Transfer14Id, @FertilizersRUsTomatoALotId, 25.0000),
	(@GreenleafGardenTenantId, @Transfer15Id, @BasilALotId, 5.0000);

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
