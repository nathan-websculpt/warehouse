
-- NOTE: represents a request and its lifecycle.

CREATE TABLE dbo.InventoryTransfers (
	InventoryTransferId INT IDENTITY (1,1) NOT NULL,
	InventoryTransferPublicId UNIQUEIDENTIFIER NOT NULL
		CONSTRAINT DF_InventoryTransfers_InventoryTransferPublicId DEFAULT NEWID(),
	TenantId INT NOT NULL,
	TransferStatusCode VARCHAR(20) NOT NULL,
	SourceLocationId INT NOT NULL,
	DestinationLocationId INT NOT NULL,
	CreatedAtUtc DATETIME2(7) NOT NULL
		CONSTRAINT DF_InventoryTransfers_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
	UpdatedAtUtc DATETIME2(7) NULL,
	RowVersion ROWVERSION NOT NULL,

	CONSTRAINT PK_InventoryTransfers
		PRIMARY KEY CLUSTERED (InventoryTransferId),

	CONSTRAINT FK_InventoryTransfers_Tenants_TenantId
		FOREIGN KEY (TenantId)
		REFERENCES dbo.Tenants (TenantId)
		ON DELETE NO ACTION,		

	CONSTRAINT FK_InventoryTransfers_WarehouseLocations_SourceLocationId
		FOREIGN KEY (TenantId, SourceLocationId)
		REFERENCES dbo.WarehouseLocations (TenantId, WarehouseLocationId)
		ON DELETE NO ACTION,

	CONSTRAINT FK_InventoryTransfers_WarehouseLocations_DestinationLocationId
		FOREIGN KEY (TenantId, DestinationLocationId)
		REFERENCES dbo.WarehouseLocations (TenantId, WarehouseLocationId)
		ON DELETE NO ACTION,

	CONSTRAINT FK_InventoryTransfers_InventoryTransferStatuses_TransferStatusCode
		FOREIGN KEY (TransferStatusCode)
		REFERENCES dbo.InventoryTransferStatuses (TransferStatusCode)
		ON DELETE NO ACTION,

	CONSTRAINT CK_InventoryTransfers_SourceAndDestinationDiffer
		CHECK (SourceLocationId <> DestinationLocationId),

	CONSTRAINT UQ_InventoryTransfers_TenantId_InventoryTransferId
		UNIQUE NONCLUSTERED (TenantId, InventoryTransferId),

	CONSTRAINT UQ_InventoryTransfers_InventoryTransferPublicId
		UNIQUE NONCLUSTERED (InventoryTransferPublicId)
);
GO