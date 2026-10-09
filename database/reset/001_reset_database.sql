USE master;
GO

--IF DB_ID(N'Warehouse') IS NOT NULL
IF DB_ID(N'WarehouseDev') IS NOT NULL
BEGIN
    --ALTER DATABASE Warehouse
    ALTER DATABASE WarehouseDev
        SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

    --DROP DATABASE Warehouse;
    DROP DATABASE WarehouseDev;
END;
GO