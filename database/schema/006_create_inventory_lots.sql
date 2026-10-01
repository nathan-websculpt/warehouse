
/*
	NOTE: a lot is a traceable batch of a product
		Product 1 -> many InventoryLots

	The same lot may have balances in multiple warehouse locations, including locations in different warehouses

	The balances determine where portions of the lot currently reside.


	NOTE: ordinary inventory movement should not change the lot. Moving units from Lot A to Lot B is a traceability correction or transformation -- not a normal warehouse move.

	NOTE: All inventory in version 1 is lot-tracked.

	FUTURE: add an explicit lot disposition for operational states such as:
		RELEASED, QUARANTINED, or RECALLED. Expiration and depletion can be derived.
*/

CREATE TABLE dbo.InventoryLots(
	InventoryLotId INT IDENTITY (1,1) NOT NULL,
	TenantId INT NOT NULL,
	ProductId INT NOT NULL,
	LotNumber NVARCHAR(100) NOT NULL,
	ManufactureDate Date NULL,
	ExpirationDate Date NULL,
	SupplierLotReference NVARCHAR(50) NULL,
	CreatedAtUtc DATETIME2(7) NOT NULL
		CONSTRAINT DF_InventoryLots_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
	UpdatedAtUtc DATETIME2(7) NULL,

	CONSTRAINT PK_InventoryLots
		PRIMARY KEY CLUSTERED (InventoryLotId),

	CONSTRAINT FK_InventoryLots_Products_TenantId_ProductId
		FOREIGN KEY (TenantId, ProductId)
		REFERENCES dbo.Products (TenantId, ProductId)
		ON DELETE NO ACTION,

	CONSTRAINT UQ_InventoryLots_TenantId_ProductId_LotNumber
		UNIQUE NONCLUSTERED (TenantId, ProductId, LotNumber),

	CONSTRAINT UQ_InventoryLots_TenantId_InventoryLotId
		UNIQUE NONCLUSTERED (TenantId, InventoryLotId),

	CONSTRAINT CK_InventoryLots_ExpirationDate_NotBeforeManufactureDate
		CHECK
		(
			ExpirationDate IS NULL
			OR ManufactureDate IS NULL
			OR ExpirationDate >= ManufactureDate
		)
);
GO