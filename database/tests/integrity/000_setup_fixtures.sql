/*

	executed in 001_run_integrity_tests
	do not execute independently

*/

-- later tests will share these IDs
DECLARE @TenantAId INT;
DECLARE @TenantBId INT;
DECLARE @WarehouseAId INT;
DECLARE @SourceLocationId INT;
DECLARE @DestinationLocationId INT;
DECLARE @UnitOfMeasureId INT;
DECLARE @ProductId INT;
DECLARE @InventoryLotAId INT;
DECLARE @InventoryLotBId INT;
DECLARE @InventoryBalanceId INT;
DECLARE @InventoryTransferId INT;
DECLARE @InventoryTransferLineId INT;
DECLARE @WarehouseAAddressId INT;

-- table for OUTPUT INSERTED
DECLARE @InsertedId TABLE
(
    Id INT
);

-- UnitOfMeasure lookup
SELECT @UnitOfMeasureId = UnitOfMeasureId
FROM dbo.UnitOfMeasure
WHERE UnitOfMeasureCode = N'EA';

IF @UnitOfMeasureId IS NULL
	THROW 51001, 'Integrity Test setup requires the EA Unit of Measure', 1;

-- Insert Tenant A
INSERT INTO dbo.Tenants (TenantName)
OUTPUT INSERTED.TenantId
INTO @InsertedId (Id)
VALUES
(
	N'Integrity Test Tenant A'
);

SELECT @TenantAId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;

-- Insert Tenant B
INSERT INTO dbo.Tenants (TenantName)
OUTPUT INSERTED.TenantId
INTO @InsertedId (Id)
VALUES
(
	N'Integrity Test Tenant B'
);

SELECT @TenantBId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;

-- Insert Warehouse A
INSERT INTO dbo.Warehouses (
	TenantId,
	WarehouseCode,
	WarehouseName
)
OUTPUT INSERTED.WarehouseId
INTO @InsertedId (Id)
VALUES
(
	@TenantAId,
	N'INTEGRITY_WH_A',
	N'Integrity Test Warehouse A'
);

SELECT @WarehouseAId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;

-- Insert source location
INSERT INTO dbo.WarehouseLocations (
	WarehouseId,
	TenantId,
	DisplayName,
	LocationCode
)
OUTPUT INSERTED.WarehouseLocationId
INTO @InsertedId (Id)
VALUES
(
	@WarehouseAId,
	@TenantAId,
	N'INTEGRITY TEST SRC LOCATION',
	N'INTEGRITY_SRC_A'
);

SELECT @SourceLocationId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;

-- Insert destination location
INSERT INTO dbo.WarehouseLocations (
	WarehouseId,
	TenantId,
	DisplayName,
	LocationCode
)
OUTPUT INSERTED.WarehouseLocationId
INTO @InsertedId (Id)
VALUES
(
	@WarehouseAId,
	@TenantAId,
	N'INTEGRITY TEST DEST LOCATION',
	N'INTEGRITY_DEST_A'
);

SELECT @DestinationLocationId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;

-- Insert one product
INSERT INTO dbo.Products (
	TenantId,
	SKU,
	Name,
	UnitOfMeasureId
)
OUTPUT INSERTED.ProductId
INTO @InsertedId (Id)
VALUES
(
	@TenantAId,
	N'TST-A-01',
	N'Tenant A, Warehouse A - Test Product 01',
	@UnitOfMeasureId
);

SELECT @ProductId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;

-- Insert two different lots for that product
-- first lot
INSERT INTO dbo.InventoryLots (
	TenantId,
	ProductId,
	LotNumber
)
OUTPUT INSERTED.InventoryLotId
INTO @InsertedId (Id)
VALUES
(
	@TenantAId,
	@ProductId,
	N'Tenant-A-Lot-1'
);

SELECT @InventoryLotAId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;

-- second lot
INSERT INTO dbo.InventoryLots (
	TenantId,
	ProductId,
	LotNumber
)
OUTPUT INSERTED.InventoryLotId
INTO @InsertedId (Id)
VALUES
(
	@TenantAId,
	@ProductId,
	N'Tenant-A-Lot-2'
);

SELECT @InventoryLotBId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;

-- Insert one valid inventory balance
INSERT INTO dbo.InventoryBalances (
	TenantId,
	WarehouseLocationId,
	InventoryLotId,
	QuantityOnHand,
	QuantityReserved
)
OUTPUT INSERTED.InventoryBalanceId
INTO @InsertedId (Id)
VALUES 
(
	@TenantAId,
	@SourceLocationId,
	@InventoryLotAId,
	10.0,
	5.0
);

SELECT @InventoryBalanceId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;

-- Insert one valid DRAFT transfer
INSERT INTO dbo.InventoryTransfers (
	TenantId,
	TransferStatusCode,
	SourceLocationId,
	DestinationLocationId
)
OUTPUT INSERTED.InventoryTransferId
INTO @InsertedId (Id)
VALUES
(
	@TenantAId,
	'DRAFT',
	@SourceLocationId,
	@DestinationLocationId
);

SELECT @InventoryTransferId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;

-- Insert one transfer line using Lot A
INSERT INTO dbo.InventoryTransferLines (
	TenantId,
	InventoryTransferId,
	InventoryLotId,
	QuantityRequested
)
OUTPUT INSERTED.InventoryTransferLineId
INTO @InsertedId (Id)
VALUES 
(
	@TenantAId,
	@InventoryTransferId,
	@InventoryLotAId,
	1.0
);

SELECT @InventoryTransferLineId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;

-- Insert one warehouse address
INSERT INTO dbo.WarehouseAddresses (
	TenantId,
	WarehouseId,
	AddressLine1,
	City,
	StateCode,
	ZipCode
)
OUTPUT INSERTED.WarehouseAddressId
INTO @InsertedId (Id)
VALUES
(
	@TenantAId,
	@WarehouseAId,
	N'123 Something St.',
	N'Atlanta',
	'GA',
	'11111-1111'
);

SELECT @WarehouseAAddressId = Id
FROM @InsertedId;

DELETE FROM @InsertedId;