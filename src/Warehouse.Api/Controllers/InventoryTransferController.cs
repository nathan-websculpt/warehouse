using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data;
using Warehouse.Contracts.InventoryTransfers;
using Warehouse.Contracts.Tenants;

namespace Warehouse.Api.Controllers;

// TODO: add to Integration Tests
[ApiController]
[Route("api/transfers")]
public sealed class InventoryTransferController(WarehouseDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InventoryTransferResponse>>> GetInventoryTransfers(CancellationToken cancellationToken)
    {
        var transfers = await dbContext.InventoryTransfers
            .AsNoTracking()
            .OrderBy(transfer => transfer.CreatedAtUtc)
            .Select(transfer => new InventoryTransferResponse(
                transfer.InventoryTransferPublicId,
                transfer.TransferStatusCode,
                transfer.SourceLocationId,
                transfer.DestinationLocationId,
                transfer.CreatedAtUtc,
                transfer.UpdatedAtUtc,
                transfer.RowVersion,
                new TenantResponse(
                    transfer.Tenant.TenantId,
                    transfer.Tenant.TenantPublicId,
                    transfer.Tenant.TenantName,
                    transfer.Tenant.IsActive,
                    transfer.Tenant.CreatedAtUtc,
                    transfer.Tenant.UpdatedAtUtc)))
            .ToListAsync(cancellationToken);

        return Ok(transfers);
    }
}
