using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Contracts.Tenants;
using Warehouse.Contracts.Warehouses;
using Warehouse.Api.Data;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController(WarehouseDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantResponse>>> GetTenants(CancellationToken cancellationToken)
    {
        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .OrderBy(tenant => tenant.TenantName)
            .Select(tenant => new TenantResponse(
                tenant.TenantPublicId,
                tenant.TenantName,
                tenant.IsActive,
                tenant.CreatedAtUtc,
                tenant.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(tenants);
    }

    [HttpGet("{tenantPublicId:guid}")]
    public async Task<ActionResult<TenantResponse>> GetTenant(Guid tenantPublicId, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.TenantPublicId == tenantPublicId)
            .Select(tenant => new TenantResponse(
                tenant.TenantPublicId,
                tenant.TenantName,
                tenant.IsActive,
                tenant.CreatedAtUtc,
                tenant.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        if(tenant is null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }

    [HttpGet("{tenantPublicId:guid}/warehouses")]
    public async Task<ActionResult<IReadOnlyList<WarehouseResponse>>> GetWarehouses(Guid tenantPublicId, CancellationToken cancellationToken)
    {
        var tenantId = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.TenantPublicId == tenantPublicId)
            .Select(tenant => (int?)tenant.TenantId)
            .SingleOrDefaultAsync(cancellationToken);

        if(tenantId is null)
        {
            return NotFound();
        }

        var warehouses = await dbContext.Warehouses
            .AsNoTracking()
            .Where(warehouse => warehouse.TenantId == tenantId.Value)
            .OrderBy(warehouse => warehouse.WarehouseCode)
            .Select(warehouse => new WarehouseResponse(
                warehouse.WarehousePublicId,
                warehouse.WarehouseCode,
                warehouse.WarehouseName,
                warehouse.IsActive,
                warehouse.CreatedAtUtc,
                warehouse.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(warehouses);
    }
}
