
-- NOTE: At this warehouse location, we currently hold this quantity from this inventory lot.

CREATE TABLE dbo.InventoryBalances (
	InventoryBalanceId INT IDENTITY (1,1) NOT NULL,
	TenantId INT NOT NULL,
	WarehouseLocationId INT NOT NULL,
	InventoryLotId INT NOT NULL,
	QuantityOnHand DECIMAL(19,4) NOT NULL
		CONSTRAINT DF_InventoryBalances_QuantityOnHand DEFAULT (0),
	QuantityReserved DECIMAL(19,4) NOT NULL
		CONSTRAINT DF_InventoryBalances_QuantityReserved DEFAULT (0),
	QuantityAvailable AS (QuantityOnHand - QuantityReserved),
	CreatedAtUtc DATETIME2(7) NOT NULL
		CONSTRAINT DF_InventoryBalances_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
	UpdatedAtUtc DATETIME2(7) NULL,
	RowVersion ROWVERSION NOT NULL,

	CONSTRAINT PK_InventoryBalances
		PRIMARY KEY CLUSTERED (InventoryBalanceId),

	CONSTRAINT FK_InventoryBalances_WarehouseLocations_TenantId_WarehouseLocationId
		FOREIGN KEY (TenantId, WarehouseLocationId)
		REFERENCES dbo.WarehouseLocations (TenantId, WarehouseLocationId)
		ON DELETE NO ACTION,

	-- current-state association between a warehouse location and an inventory lot
	CONSTRAINT FK_InventoryBalances_InventoryLots_TenantId_InventoryLotId
		FOREIGN KEY (TenantId, InventoryLotId)
		REFERENCES dbo.InventoryLots (TenantId, InventoryLotId)
		ON DELETE NO ACTION,

	CONSTRAINT UQ_InventoryBalances_TenantId_WarehouseLocationId_InventoryLotId
		UNIQUE NONCLUSTERED (TenantId, WarehouseLocationId, InventoryLotId),

	CONSTRAINT CK_InventoryBalances_QuantityOnHand_NonNegative
		CHECK (QuantityOnHand >= 0),

	CONSTRAINT CK_InventoryBalances_QuantityReserved_NonNegative
		CHECK (QuantityReserved >= 0),

	CONSTRAINT CK_InventoryBalances_QuantityReserved_NotGreaterThanOnHand
		CHECK (QuantityReserved <= QuantityOnHand)
);
GO