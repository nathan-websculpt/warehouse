
-- operational contact information

CREATE TABLE dbo.WarehouseContacts (
	WarehouseContactId INT IDENTITY (1,1) NOT NULL,
	TenantId INT NOT NULL,
	WarehouseId INT NOT NULL,
	ContactName NVARCHAR(100) NOT NULL,
	ContactRole NVARCHAR(100) NOT NULL,
	PrimaryPhoneNumber VARCHAR (20) NOT NULL,
	PrimaryPhoneExtension VARCHAR(10) NULL,
	SecondaryPhoneNumber  VARCHAR (20) NULL,
	SecondaryPhoneExtension VARCHAR(10) NULL,
	EmailAddress NVARCHAR(254) NOT NULL,
	IsActive BIT NOT NULL
		CONSTRAINT DF_WarehouseContacts_IsActive DEFAULT (1),
	CreatedAtUtc DATETIME2(7) NOT NULL
		CONSTRAINT DF_WarehouseContacts_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
	UpdatedAtUtc DATETIME2(7) NULL,
	
	CONSTRAINT PK_WarehouseContacts
		PRIMARY KEY CLUSTERED (WarehouseContactId),		

	CONSTRAINT FK_WarehouseContacts_Warehouses_TenantId_WarehouseId
		FOREIGN KEY (TenantId, WarehouseId)
		REFERENCES dbo.Warehouses (TenantId, WarehouseId)
		ON DELETE NO ACTION
);
GO