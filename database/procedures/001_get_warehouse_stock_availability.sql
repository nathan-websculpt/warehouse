SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.GetWarehouseStockAvailability
    @TenantId INT,
    @WarehousePublicId UNIQUEIDENTIFIER,
    @LowStockThreshold DECIMAL(19, 4) = 20.0000
AS
BEGIN

    SET NOCOUNT ON;

    -- parameter checks
    IF @TenantId IS NULL
    BEGIN
        THROW 51001, N'TenantId is required.', 1;
    END;

    IF @WarehousePublicId IS NULL
    BEGIN
        THROW 51002, N'WarehousePublicId is required.', 1;
    END;

    -- omitting this parameter uses its default - passing NULL does not
    IF @LowStockThreshold IS NULL OR @LowStockThreshold < 0
    BEGIN
        THROW 51003, N'LowStockThreshold must be non-NULL and nonnegative.', 1;
    END;

    -- IsActive is NOT NULL in the table, so a NULL variable means no row was found 
    -- This distinguishes a missing tenant from an inactive one
    DECLARE @TenantIsActive BIT;

    SELECT @TenantIsActive = T.IsActive
    FROM dbo.Tenants AS T
    WHERE T.TenantId = @TenantId;

    IF @TenantIsActive IS NULL
    BEGIN
        THROW 51004, N'Tenant not found.', 1;
    END;

    IF @TenantIsActive = 0
    BEGIN
        THROW 51005, N'Tenant must be active.', 1;
    END;

    DECLARE @WarehouseId INT;
    DECLARE @WarehouseIsActive BIT;

    -- Resolve the public GUID to its internal identity using BOTH inputs - a warehouse owned by another tenant fails this lookup
    SELECT
        @WarehouseId = W.WarehouseId,
        @WarehouseIsActive = W.IsActive
    FROM dbo.Warehouses AS W
    WHERE W.TenantId = @TenantId
      AND W.WarehousePublicId = @WarehousePublicId;

    IF @WarehouseId IS NULL
    BEGIN
        THROW 51006, N'Warehouse not found for this tenant.', 1;
    END;

    IF @WarehouseIsActive = 0
    BEGIN
        THROW 51007, N'Warehouse must be active.', 1;
    END;

    -- capture date once so every balance uses same report date
    -- NOTE: a lot expiring ON this date remains eligible
    DECLARE @ReportDateUtc DATE = CONVERT(DATE, SYSUTCDATETIME());

      
    ;WITH WarehouseBalanceFacts AS
    (
        -- start with actual balances and restrict them to this warehouse
        -- keep inactive and expired stock - report needs their totals
        SELECT
            B.TenantId,
            IL.ProductId,
            B.InventoryLotId,
            B.QuantityOnHand,
            B.QuantityReserved,
            B.QuantityAvailable,
            CASE
                -- first match wins... A closed/expired balance belongs only to INACTIVE_LOCATION, never to both buckets
                WHEN L.IsActive = 0
                    THEN 'INACTIVE_LOCATION'
                WHEN IL.ExpirationDate < @ReportDateUtc
                    THEN 'EXPIRED'
                -- NULL expiration does not satisfy the comparison above.
                -- so it falls here along with today/future dates.
                ELSE 'ELIGIBLE'
            END AS StockBucket
        FROM dbo.InventoryBalances AS B
        INNER JOIN dbo.WarehouseLocations AS L
            ON L.TenantId = B.TenantId
           AND L.WarehouseLocationId = B.WarehouseLocationId
        INNER JOIN dbo.InventoryLots AS IL
            ON IL.TenantId = B.TenantId
           AND IL.InventoryLotId = B.InventoryLotId
        WHERE B.TenantId = @TenantId
          AND L.WarehouseId = @WarehouseId
    ),
    ProductStockTotals AS
    (
        -- reduce the balance facts to one row per tenant/product
        SELECT
            F.TenantId,
            F.ProductId,
            SUM(F.QuantityOnHand) AS PhysicalOnHand,
            SUM(CASE
                WHEN F.StockBucket = 'ELIGIBLE' THEN F.QuantityOnHand
                ELSE 0
            END) AS EligibleOnHand,
            SUM(CASE
                WHEN F.StockBucket = 'ELIGIBLE' THEN F.QuantityReserved
                ELSE 0
            END) AS EligibleReserved,
            -- QuantityAvailable is the existing computed column:
            --      QuantityOnHand - QuantityReserved, for each balance
            -- Summing per-balance availability preserves all four decimal places
            -- subtracting two decimal(38,4) totals can reduce scale
            SUM(CASE
                WHEN F.StockBucket = 'ELIGIBLE' THEN F.QuantityAvailable
                ELSE 0
            END) AS AvailableToPick,
            -- excluded buckets report PHYSICAL on-hand quantities
            --      reservations do not reduce either excluded-stock total
            SUM(CASE
                WHEN F.StockBucket = 'EXPIRED' THEN F.QuantityOnHand
                ELSE 0
            END) AS ExpiredOnHand,
            SUM(CASE
                WHEN F.StockBucket = 'INACTIVE_LOCATION' THEN F.QuantityOnHand
                ELSE 0
            END) AS InactiveLocationOnHand,
            -- noneligible rows produce NULL, which COUNT ignores
            -- DISTINCT counts a lot once across multiple locations
            -- there is no quantity filter: zero and fully reserved eligible balances still establish an eligible lot
            COUNT(DISTINCT CASE
                WHEN F.StockBucket = 'ELIGIBLE' THEN F.InventoryLotId
            END) AS EligibleLotCount
        FROM WarehouseBalanceFacts AS F
        GROUP BY F.TenantId, F.ProductId
    )
    SELECT
        P.ProductPublicId,
        P.SKU,
        P.Name AS ProductName,
        U.UnitOfMeasureCode,
        @ReportDateUtc AS ReportDateUtc,
        -- unmatched LEFT JOIN gives NULL totals - return zero instead
        ISNULL(S.PhysicalOnHand, 0) AS PhysicalOnHand,
        ISNULL(S.EligibleOnHand, 0) AS EligibleOnHand,
        ISNULL(S.EligibleReserved, 0) AS EligibleReserved,
        ISNULL(S.AvailableToPick, 0) AS AvailableToPick,
        ISNULL(S.ExpiredOnHand, 0) AS ExpiredOnHand,
        ISNULL(S.InactiveLocationOnHand, 0) AS InactiveLocationOnHand,
        ISNULL(S.EligibleLotCount, 0) AS EligibleLotCount,
        CASE
            -- check zero first: it remains OUT_OF_STOCK even when low-stock threshold is also zero
            WHEN ISNULL(S.AvailableToPick, 0) = 0
                THEN 'OUT_OF_STOCK'
            -- this branch has positive availability. Strictly less than the threshold means LOW_STOCK
            --      equality means OK
            WHEN S.AvailableToPick < @LowStockThreshold
                THEN 'LOW_STOCK'
            ELSE 'OK'
        END AS StockStatus
    FROM dbo.Products AS P
    INNER JOIN dbo.UnitOfMeasure AS U
        ON U.UnitOfMeasureId = P.UnitOfMeasureId
    -- Products drive the report...
    -- LEFT JOIN retains products with no lots, no balances, or balances only in other warehouses
    LEFT JOIN ProductStockTotals AS S
        ON S.TenantId = P.TenantId
       AND S.ProductId = P.ProductId
    WHERE P.TenantId = @TenantId
      AND P.IsActive = 1
    ORDER BY P.SKU, P.ProductPublicId;
END;
GO
