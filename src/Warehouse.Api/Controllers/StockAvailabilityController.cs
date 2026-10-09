using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Contracts.StockAvailability;
using Warehouse.Api.Data;
using Warehouse.Api.Data.Queries;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/tenants/{tenantPublicId:guid}/warehouses/{warehousePublicId:guid}/stock-availability")]
public sealed class StockAvailabilityController(WarehouseDbContext dbContext, StockAvailabilityQuery stockAvailabilityQuery) : ControllerBase
{
    [HttpGet]
    public async Task<
        ActionResult<IReadOnlyList<StockAvailabilityResponse>>>
        GetStockReadiness(
            Guid tenantPublicId,
            Guid warehousePublicId,
            decimal lowStockThreshold = 20.0000m,
            CancellationToken cancellationToken = default)
    {
        if (lowStockThreshold < 0)
        {
            return BadRequest(
                "Low-stock threshold cannot be negative.");
        }

        // EF Core - translate the public tenant ID from the HTTP API
        //      into the internal TenantId required by the sproc
        var tenantId = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant =>
                tenant.TenantPublicId == tenantPublicId &&
                tenant.IsActive)
            .Select(tenant => (int?)tenant.TenantId)
            .SingleOrDefaultAsync(cancellationToken);

        if (tenantId == null)
        {
            return NotFound();
        }

        // EF Core - verify that warehouse belongs to this tenant
        var warehouseExists = await dbContext.Warehouses
            .AsNoTracking()
            .AnyAsync(
                warehouse =>
                    warehouse.TenantId == tenantId.Value &&
                    warehouse.WarehousePublicId ==
                        warehousePublicId &&
                    warehouse.IsActive,
                cancellationToken);

        if (!warehouseExists)
        {
            return NotFound();
        }

        // SqlClient - execute dbo.GetWarehouseStockAvailability
        var report =
            await stockAvailabilityQuery.ExecuteAsync(
                tenantId.Value,
                warehousePublicId,
                lowStockThreshold,
                cancellationToken);

        return Ok(report);
    }
}