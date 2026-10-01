
/*
	NOTE for future: model product/package dimensions, weight, and UOM conversions together.
*/

CREATE TABLE dbo.Products (
	ProductId INT IDENTITY(1,1) NOT NULL,
	ProductPublicId UNIQUEIDENTIFIER NOT NULL
		CONSTRAINT DF_Products_ProductPublicId DEFAULT NEWID(),
	TenantId INT NOT NULL,
	SKU NVARCHAR(100) NOT NULL,
	Name NVARCHAR(100) NOT NULL,
	Description NVARCHAR(1000) NULL,
	UnitOfMeasureId INT NOT NULL,
	IsActive BIT NOT NULL
		CONSTRAINT DF_Products_IsActive DEFAULT (1),
	CreatedAtUtc DATETIME2(7) NOT NULL
		CONSTRAINT DF_Products_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
	UpdatedAtUtc DATETIME2(7) Null,

	CONSTRAINT PK_Products
		PRIMARY KEY CLUSTERED (ProductId),

	CONSTRAINT FK_Products_Tenants_TenantId
		FOREIGN KEY (TenantId)
		REFERENCES dbo.Tenants (TenantId)
		ON DELETE NO ACTION,

	CONSTRAINT FK_Products_UnitOfMeasure_UnitOfMeasureId
		FOREIGN KEY (UnitOfMeasureId)
		REFERENCES dbo.UnitOfMeasure (UnitOfMeasureId)
		ON DELETE NO ACTION,

	CONSTRAINT UQ_Products_ProductPublicId
		UNIQUE NONCLUSTERED (ProductPublicId),

	CONSTRAINT UQ_Products_TenantId_ProductId
		UNIQUE NONCLUSTERED (TenantId, ProductId),

	CONSTRAINT UQ_Products_TenantId_SKU
		UNIQUE NONCLUSTERED (TenantId, SKU)
);
GO