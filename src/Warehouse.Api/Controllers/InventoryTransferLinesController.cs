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
    public async Task<ActionResult<IReadOnlyList<InventoryTransferLineResponse>>> GetInventoryTransferLines(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] int? tenantId = null,
        [FromQuery] Guid? inventoryTransferPublicId = null)
    {
        q = q?.Trim();
        if(q?.Length > 100)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Search text can not exceed 100 characters.");
        }

        var query = dbContext.InventoryTransferLines.AsNoTracking();

        if(tenantId.HasValue)
        {
            query = query.Where(transferLine => transferLine.TenantId == tenantId.Value);
        }

        if(inventoryTransferPublicId.HasValue)
        {
            query = query.Where(transferLine => transferLine.InventoryTransfer.InventoryTransferPublicId == inventoryTransferPublicId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var isTransferId = Guid.TryParse(q, out var transferId);
            var isNumber = int.TryParse(q, out var number);

            query = query.Where(transferLine =>
                transferLine.InventoryTransfer.Tenant.TenantName.Contains(q) ||
                transferLine.InventoryTransfer.TransferStatusCode.Contains(q) ||
                (isTransferId && transferLine.InventoryTransfer.InventoryTransferPublicId == transferId) ||
                (isNumber && (transferLine.InventoryTransferLineId == number || transferLine.InventoryLotId == number)));
        }

        var transferLines = await query
            .OrderBy(transferLine => transferLine.CreatedAtUtc)
            .ThenBy(transferLine => transferLine.InventoryTransferLineId)
            .Select(transferLine => new InventoryTransferLineResponse(
                transferLine.CreatedAtUtc,
                transferLine.QuantityRequested,
                transferLine.InventoryTransferLineId,
                transferLine.InventoryLotId,
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
