
CREATE TABLE dbo.Warehouses
(
    WarehouseId INT IDENTITY(1, 1) NOT NULL,
    TenantId INT NOT NULL,
    WarehousePublicId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Warehouses_WarehousePublicId DEFAULT NEWID(),
    WarehouseCode NVARCHAR(50) NOT NULL,
    WarehouseName NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL
        CONSTRAINT DF_Warehouses_IsActive DEFAULT (1),
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_Warehouses_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2(7) NULL,

    CONSTRAINT PK_Warehouses
        PRIMARY KEY CLUSTERED (WarehouseId),

    CONSTRAINT FK_Warehouses_Tenants_TenantId
        FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (TenantId)
        ON DELETE NO ACTION,

    CONSTRAINT UQ_Warehouses_WarehousePublicId
        UNIQUE NONCLUSTERED (WarehousePublicId),

    CONSTRAINT UQ_Warehouses_TenantId_WarehouseCode
        UNIQUE NONCLUSTERED (TenantId, WarehouseCode),

    CONSTRAINT UQ_Warehouses_TenantId_WarehouseId
        UNIQUE NONCLUSTERED (TenantId, WarehouseId)
);
GO