
CREATE TABLE dbo.InventoryTransferLines (
	InventoryTransferLineId INT IDENTITY (1,1) NOT NULL,
	TenantId INT NOT NULL,
	InventoryTransferId INT NOT NULL,
	InventoryLotId INT NOT NULL,
	QuantityRequested DECIMAL(19,4) NOT NULL,	
	CreatedAtUtc DATETIME2(7) NOT NULL
		CONSTRAINT DF_InventoryTransferLines_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

	CONSTRAINT PK_InventoryTransferLines
		PRIMARY KEY CLUSTERED (InventoryTransferLineId),

	CONSTRAINT FK_InventoryTransferLines_Tenants_TenantId
		FOREIGN KEY (TenantId)
		REFERENCES dbo.Tenants (TenantId)
		ON DELETE NO ACTION,

	CONSTRAINT FK_InventoryTransferLines_InventoryTransfers_TenantId_InventoryTransferId
		FOREIGN KEY (TenantId, InventoryTransferId)
		REFERENCES dbo.InventoryTransfers (TenantId, InventoryTransferId)
		ON DELETE NO ACTION,

	CONSTRAINT FK_InventoryTransferLines_InventoryLots_TenantId_InventoryLotId
		FOREIGN KEY (TenantId, InventoryLotId)
		REFERENCES dbo.InventoryLots (TenantId, InventoryLotId)
		ON DELETE NO ACTION,

	CONSTRAINT UQ_InventoryTransferLines_TenantId_InventoryTransferId_InventoryLotId
		UNIQUE NONCLUSTERED (TenantId, InventoryTransferId, InventoryLotId),

	CONSTRAINT CK_InventoryTransferLines_QuantityRequested_Positive
		CHECK (QuantityRequested > 0),

	CONSTRAINT UQ_InventoryTransferLines_TenantId_InventoryTransferLineId_InventoryLotId
		UNIQUE NONCLUSTERED (TenantId, InventoryTransferLineId, InventoryLotId)
);
GO