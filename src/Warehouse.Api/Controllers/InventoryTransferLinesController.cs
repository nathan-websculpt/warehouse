using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Infrastructure;
using Warehouse.Contracts.Paging;
using Warehouse.Contracts.Errors;
using Warehouse.Api.Data;
using Warehouse.Contracts.InventoryTransferLines;
using Warehouse.Contracts.InventoryTransfers;
using Warehouse.Contracts.Tenants;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/transfer-lines")]
public sealed class InventoryTransferLinesController(WarehouseDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<InventoryTransferLineResponse>>> GetInventoryTransferLines(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] int? tenantId = null,
        [FromQuery] Guid? inventoryTransferPublicId = null,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? cursor = null)
    {
        q = q?.Trim();
        if(q?.Length > 100)  
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Search text can not exceed 100 characters.");

        if (pageSize is < 1 or > 100)
            return ApiProblems.Result(400, ErrorCodes.Validation, "Page size must be between 1 and 100.");
        

        if (Request.Query.TryGetValue(nameof(cursor), out var cursorText))
            cursor = cursorText.ToString();

        var context = TransferCursor.ContextFor("transfer-lines", q, tenantId, inventoryTransferPublicId, pageSize);
         
        if (!TransferCursor.TryRead(cursor, context, out var boundary))
            return ApiProblems.Result(400, ErrorCodes.InvalidCursor, "The page cursor is invalid for this search. Start a new search.");

        var query = dbContext.InventoryTransferLines.AsNoTracking();

        if(tenantId.HasValue)
            query = query.Where(transferLine => transferLine.TenantId == tenantId.Value);  

        if(inventoryTransferPublicId.HasValue)
            query = query.Where(transferLine => transferLine.InventoryTransfer.InventoryTransferPublicId == inventoryTransferPublicId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var isTransferId = Guid.TryParse(q, out var transferId);
               
            query = query.Where(transferLine =>
                transferLine.InventoryTransfer.Tenant.TenantName.Contains(q) ||
                transferLine.InventoryTransfer.TransferStatusCode.Contains(q) ||
                (
                    isTransferId && transferLine.InventoryTransfer.InventoryTransferPublicId == transferId) ||
                    transferLine.InventoryLot.Product.Sku.Contains(q) || 
                    transferLine.InventoryLot.Product.Name.Contains(q) || 
                    transferLine.InventoryLot.LotNumber.Contains(q)
                );
        }

        if (boundary is not null)
        {
            query = boundary.Backwards
                ? query.Where(line => line.CreatedAtUtc < boundary.CreatedAtUtc || (line.CreatedAtUtc == boundary.CreatedAtUtc && line.InventoryTransferLineId < boundary.Id))
                : query.Where(line => line.CreatedAtUtc > boundary.CreatedAtUtc || (line.CreatedAtUtc == boundary.CreatedAtUtc && line.InventoryTransferLineId > boundary.Id));
        }

        var ordered = boundary?.Backwards == true
            ? query.OrderByDescending(line => line.CreatedAtUtc).ThenByDescending(line => line.InventoryTransferLineId)
            : query.OrderBy(line => line.CreatedAtUtc).ThenBy(line => line.InventoryTransferLineId);

        var transferLines = await ordered.Take(pageSize + 1)
            .Select(transferLine => 
                new CursorRow<InventoryTransferLineResponse>(
                    transferLine.CreatedAtUtc,
                    transferLine.InventoryTransferLineId,
                    new InventoryTransferLineResponse(
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
                                transferLine.InventoryTransfer.Tenant.UpdatedAtUtc)))))
            .ToListAsync(cancellationToken);
          
        return Ok(CursorPage.Create(transferLines, pageSize, boundary, context));
    }


}