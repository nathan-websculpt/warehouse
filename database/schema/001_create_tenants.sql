
CREATE TABLE dbo.Tenants
(
    TenantId INT IDENTITY(1, 1) NOT NULL,
    TenantPublicId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Tenants_TenantPublicId DEFAULT NEWID(),
    TenantName NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL
        CONSTRAINT DF_Tenants_IsActive DEFAULT (1),
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_Tenants_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2(7) NULL,

    CONSTRAINT PK_Tenants
        PRIMARY KEY CLUSTERED (TenantId),

    CONSTRAINT UQ_Tenants_TenantPublicId
        UNIQUE NONCLUSTERED (TenantPublicId)
);
GO