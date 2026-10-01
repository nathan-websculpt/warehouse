
-- V1 allows for one current physical address per warehouse

CREATE TABLE dbo.WarehouseAddresses(
	WarehouseAddressId INT IDENTITY (1,1) NOT NULL,
	TenantId INT NOT NULL,
	WarehouseId INT NOT NULL,
	AddressLine1 NVARCHAR(100) NOT NULL,
	AddressLine2 NVARCHAR(100) NULL,
	City NVARCHAR(100) NOT NULL,
	StateCode CHAR(2) NOT NULL,
	ZipCode VARCHAR(10) NOT NULL,
	CreatedAtUtc DATETIME2(7) NOT NULL
		CONSTRAINT DF_WarehouseAddresses_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
	UpdatedAtUtc DATETIME2(7) NULL,	

	CONSTRAINT PK_WarehouseAddresses
		PRIMARY KEY CLUSTERED (WarehouseAddressId),

	CONSTRAINT FK_WarehouseAddresses_Warehouses_TenantId_WarehouseId
		FOREIGN KEY (TenantId, WarehouseId)
		REFERENCES dbo.Warehouses (TenantId, WarehouseId)
		ON DELETE NO ACTION,

	CONSTRAINT FK_WarehouseAddresses_States_StateCode
		FOREIGN KEY (StateCode)
		REFERENCES dbo.States (StateCode)
		ON DELETE NO ACTION,

	CONSTRAINT UQ_WarehouseAddresses_TenantId_WarehouseId
		UNIQUE NONCLUSTERED (TenantId, WarehouseId)
);
GO