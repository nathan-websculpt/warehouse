using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.Xml;
using Warehouse.Api.Data;
using Warehouse.Contracts.InventoryTransferLines;
using Warehouse.Contracts.InventoryTransfers;
using Warehouse.Contracts.Tenants;

namespace Warehouse.Api.Controllers;

// TODO: add to Integration Tests
[ApiController]
[Route("api/transfer-lines")]
public sealed class InventoryTransferLinesController(WarehouseDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InventoryTransferLineResponse>>> GetInventoryTransferLines(CancellationToken cancellationToken)
    {
        var transferLines = await dbContext.InventoryTransferLines
            .AsNoTracking()
            .OrderBy(transferLine => transferLine.CreatedAtUtc)
            .Select(transferLine => new InventoryTransferLineResponse(
                transferLine.CreatedAtUtc,
                transferLine.QuantityRequested,
                new InventoryTransferResponse(
                    transferLine.InventoryTransfer.InventoryTransferPublicId,
                    transferLine.InventoryTransfer.TransferStatusCode,
                    transferLine.InventoryTransfer.SourceLocationId,
                    transferLine.InventoryTransfer.DestinationLocationId,
                    transferLine.InventoryTransfer.CreatedAtUtc,
                    transferLine.InventoryTransfer.UpdatedAtUtc,
                    transferLine.InventoryTransfer.RowVersion,
                    new TenantResponse(
                        transferLine.InventoryTransfer.Tenant.TenantId,
                        transferLine.InventoryTransfer.Tenant.TenantPublicId,
                        transferLine.InventoryTransfer.Tenant.TenantName,
                        transferLine.InventoryTransfer.Tenant.IsActive,
                        transferLine.InventoryTransfer.Tenant.CreatedAtUtc,
                        transferLine.InventoryTransfer.Tenant.UpdatedAtUtc))))
            .ToListAsync(cancellationToken);

        return Ok(transferLines);
    }
}
