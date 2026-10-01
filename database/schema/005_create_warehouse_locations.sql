
CREATE TABLE dbo.WarehouseLocations (
	WarehouseLocationId INT IDENTITY(1,1) NOT NULL,
	WarehouseId INT NOT NULL,
	TenantId INT NOT NULL,
	DisplayName NVARCHAR(100) NULL,
	LocationCode NVARCHAR(50) NOT NULL, -- example: A-04-12, RECEIVING-01, SHIPPING-02, QUARANTINE	
	IsActive BIT NOT NULL
		CONSTRAINT DF_WarehouseLocations_IsActive DEFAULT (1),
	CreatedAtUtc DATETIME2(7) NOT NULL
		CONSTRAINT DF_WarehouseLocations_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
	UpdatedAtUtc DATETIME2(7) NULL,

	CONSTRAINT PK_WarehouseLocations
		PRIMARY KEY CLUSTERED (WarehouseLocationId),

	CONSTRAINT FK_WarehouseLocations_Warehouses_TenantId_WarehouseId
		FOREIGN KEY (TenantId, WarehouseId)
		REFERENCES dbo.Warehouses (TenantId, WarehouseId)
		ON DELETE NO ACTION,

	CONSTRAINT UQ_WarehouseLocations_TenantId_WarehouseLocationId
		UNIQUE NONCLUSTERED (TenantId, WarehouseLocationId),

	CONSTRAINT UQ_WarehouseLocations_TenantId_WarehouseId_LocationCode
		UNIQUE NONCLUSTERED (TenantId, WarehouseId, LocationCode)
);
GO