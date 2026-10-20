using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using Warehouse.Api.Data;

namespace Warehouse.Api.Infrastructure;

/*
        custom ASP.NET Core health check 
            verifies that the API can connect to the DB
            verifies that specific DB objects are installed
        
        implements IHealthCheck, so Core calls its CheckHealthAsync() when evaluating app health
            check receives SqlConnectionFactory through DI, opens a conn, executes a query that checks:
                tables are present,
                sprocs exist,
                protection triggers

        registered in Program.cs
            Aspire Service Details uses this in the /health endpoint
            Aspire AppHost polls the API's /health endpoint
                Blazor also calls .WaitFor(api), so DB Readiness contributes to when Aspire considers the API ready for Blazor App


        This suppresses OpenTelemetry instrumentation during the probe
            using var suppression = SuppressInstrumentationScope.Begin();
                this prevents repeated health polling from filling dashboard with SQL spans
 */

public sealed class WarehouseReadiness(SqlConnectionFactory connections) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // suppress SQL spans in the dashboard
        using var suppression = SuppressInstrumentationScope.Begin();
          
        try
        {
            await using var connection = connections.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("""
                SELECT CASE WHEN
                    NOT EXISTS
                    (
                        SELECT required.Name 
                        FROM 
                        (
                            VALUES
                            ('Tenants'),
                            ('Warehouses'),
                            ('WarehouseLocations'),
                            ('Products'),
                            ('UnitOfMeasure'),
                            ('InventoryLots'),
                            ('InventoryBalances'),
                            ('InventoryTransfers'),
                            ('InventoryTransferLines'),
                            ('InventoryMovements')
                        ) AS required(Name)
                        WHERE OBJECT_ID('dbo.' + required.Name, 'U') IS NULL
                    )
                    AND OBJECT_ID('dbo.GetWarehouseStockAvailability', 'P') IS NOT NULL
                    AND OBJECT_ID('dbo.CompleteInventoryTransfer', 'P') IS NOT NULL
                    AND EXISTS 
                    (
                        SELECT 1
                        FROM sys.parameters
                        WHERE object_id = OBJECT_ID('dbo.CompleteInventoryTransfer')
                            AND name = '@RetrieveOnly'
                            AND system_type_id = TYPE_ID('bit')
                    )
                    AND 
                    (
                        SELECT COUNT(*)
                        FROM sys.triggers
                        WHERE parent_id IN (OBJECT_ID('dbo.InventoryTransfers'), OBJECT_ID('dbo.InventoryTransferLines'))
                            AND name IN ('TR_InventoryTransfers_ProtectContent', 'TR_InventoryTransferLines_ProtectContent') 
                            AND is_disabled = 0
                            AND is_not_for_replication = 0
                    ) = 2
                    THEN 1 ELSE 0 END;
                """, connection) { CommandTimeout = 3 };

               
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1
                ? HealthCheckResult.Healthy("Warehouse database is ready.")
                : HealthCheckResult.Unhealthy("Required warehouse schema is missing.");
        }
        catch (Exception exception) 
            when (exception is SqlException or OperationCanceledException)
        {
            // never put connection details into the public health result   
            return HealthCheckResult.Unhealthy("Warehouse database readiness could not be confirmed.");
        }
    }
}
