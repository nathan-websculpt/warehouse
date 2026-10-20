using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Contracts.StockAvailability;
using Warehouse.Api.Data;
using Warehouse.Api.Data.Queries;
using Warehouse.Api.Infrastructure;
using Warehouse.Contracts.Errors;

namespace Warehouse.Api.Controllers;

[ApiController]
[SqlOperation(SqlOperation.StockAvailability)]
[Route("api/tenants/{tenantPublicId:guid}/warehouses/{warehousePublicId:guid}/stock-availability")]
public sealed class StockAvailabilityController(WarehouseDbContext dbContext, StockAvailabilityQuery stockAvailabilityQuery) : ControllerBase
{
    [HttpGet]
    public async Task<
        ActionResult<IReadOnlyList<StockAvailabilityResponse>>>
        GetStockReadiness(
            Guid tenantPublicId,
            Guid warehousePublicId,
            [FromQuery] string? lowStockThreshold = null,  
            CancellationToken cancellationToken = default)
    {
        if (Request.Query.TryGetValue(nameof(lowStockThreshold), out var thresholdText))
            lowStockThreshold = thresholdText.ToString();
        
        if (!StockThreshold.TryParse(lowStockThreshold, out var threshold))
        {
            ModelState.AddModelError(nameof(lowStockThreshold), StockThreshold.ValidationMessage); 
            var problem = new ValidationProblemDetails(ModelState)  { Status = 400, Extensions = { ["code"] = ErrorCodes.InvalidThreshold } };
            return BadRequest(problem); 
        }

        // EF - translate the public tenant ID from the HTTP API
        //      into the internal TenantId required by the sproc
        var tenantId = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.TenantPublicId == tenantPublicId && tenant.IsActive)
            .Select(tenant => (int?)tenant.TenantId)
            .SingleOrDefaultAsync(cancellationToken);

        if (tenantId == null)
            return NotFound();

        // EF - verify that warehouse belongs to this tenant
        var warehouseExists = await dbContext.Warehouses
            .AsNoTracking()
            .AnyAsync(
                warehouse =>
                    warehouse.TenantId == tenantId.Value &&
                    warehouse.WarehousePublicId == warehousePublicId &&
                    warehouse.IsActive,
                cancellationToken);

        if (!warehouseExists)
            return NotFound();

        // SqlClient - execute dbo.GetWarehouseStockAvailability
        var report = await stockAvailabilityQuery.ExecuteAsync(
            tenantId.Value,
            warehousePublicId,
            threshold,
            cancellationToken);

        return Ok(report);
    }
} 