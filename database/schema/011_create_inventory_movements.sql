
/*
	V1 movements originate only from inventory transfer lines

	FUTURE: Add enforceable relationships when receipt, shipment, and adjustment workflows are designed
*/

CREATE TABLE dbo.InventoryMovements (
	InventoryMovementId INT IDENTITY (1,1) NOT NULL,
	TenantId INT NOT NULL,
	InventoryTransferLineId INT NOT NULL,
	InventoryLotId INT NOT NULL,
	Quantity DECIMAL(19,4) NOT NULL,
	SourceLocationId INT NOT NULL,
	DestinationLocationId INT NOT NULL,
	CreatedAtUtc DATETIME2(7) NOT NULL
		CONSTRAINT DF_InventoryMovements_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

	CONSTRAINT PK_InventoryMovements
		PRIMARY KEY CLUSTERED (InventoryMovementId),

	CONSTRAINT FK_InventoryMovements_InventoryLots_TenantId_InventoryLotId
		FOREIGN KEY (TenantId, InventoryLotId)
		REFERENCES dbo.InventoryLots (TenantId, InventoryLotId)
		ON DELETE NO ACTION,

	CONSTRAINT FK_InventoryMovements_InventoryTransferLines_Tenant_Line_Lot
		FOREIGN KEY (TenantId, InventoryTransferLineId, InventoryLotId)
		REFERENCES dbo.InventoryTransferLines (TenantId, InventoryTransferLineId, InventoryLotId)
		ON DELETE NO ACTION,

	CONSTRAINT FK_InventoryMovements_WarehouseLocations_TenantId_SourceLocationId
		FOREIGN KEY (TenantId, SourceLocationId)
		REFERENCES dbo.WarehouseLocations (TenantId, WarehouseLocationId)
		ON DELETE NO ACTION,

	CONSTRAINT FK_InventoryMovements_WarehouseLocations_TenantId_DestinationLocationId
		FOREIGN KEY (TenantId, DestinationLocationId)
		REFERENCES dbo.WarehouseLocations (TenantId, WarehouseLocationId)
		ON DELETE NO ACTION,

	-- movement checks
	CONSTRAINT CK_InventoryMovements_Quantity_Positive
		CHECK (Quantity > 0),

	CONSTRAINT CK_InventoryMovements_SourceAndDestinationDiffer
		CHECK (SourceLocationId <> DestinationLocationId)
);
GO