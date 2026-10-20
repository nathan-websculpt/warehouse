using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data;
using Warehouse.Contracts.InventoryTransfers;
using Warehouse.Contracts.Tenants;
using Warehouse.Contracts.Paging;
using Warehouse.Contracts.Errors;
using Warehouse.Api.Infrastructure;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/transfers")]
public sealed class InventoryTransferController(WarehouseDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Searches transfer headers, optionally restricted to one tenant.
    /// Matches tenant name, status, SKU, product name, lot number, or a full transfer GUID.
    /// Returns each matching transfer once, including transfers with no lines when their header matches.
    /// </summary>
    /// <returns>A page of matching headers with opaque Previous/Next cursors.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<InventoryTransferResponse>>>
        GetInventoryTransfers(
            CancellationToken cancellationToken,
            [FromQuery] string? q = null,
            [FromQuery] int? tenantId = null,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? cursor = null)
    {
        q = q?.Trim();
        if (q?.Length > 100)
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Search text can not exceed 100 characters.");

        if (pageSize is < 1 or > 100)
            return ApiProblems.Result(400, ErrorCodes.Validation, "Page size must be between 1 and 100.");   
        
        if (Request.Query.TryGetValue(nameof(cursor), out var cursorText))
            cursor = cursorText.ToString();

        var context = TransferCursor.ContextFor("transfers", q, tenantId, null, pageSize);

        if (!TransferCursor.TryRead(cursor, context, out var boundary))
            return ApiProblems.Result(400, ErrorCodes.InvalidCursor, "The page cursor is invalid for this search. Start a new search.");


        var query = dbContext.InventoryTransfers.AsNoTracking();

        if (tenantId.HasValue)
            query = query.Where(transfer => transfer.TenantId == tenantId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var isTransferId = Guid.TryParse(q, out var transferPublicId);

            query = query.Where
                (
                    transfer => transfer.Tenant.TenantName.Contains(q) ||
                    transfer.TransferStatusCode.Contains(q) ||
                    (
                        isTransferId && transfer.InventoryTransferPublicId == transferPublicId
                    ) || transfer.InventoryTransferLines.Any
                            (
                                line => line.InventoryLot.Product.Sku.Contains(q) ||
                                        line.InventoryLot.Product.Name.Contains(q) ||
                                        line.InventoryLot.LotNumber.Contains(q)
                            )
                );
        }

        if (boundary is not null)
        {
            query = boundary.Backwards
                ? query.Where(transfer => transfer.CreatedAtUtc < boundary.CreatedAtUtc || (transfer.CreatedAtUtc == boundary.CreatedAtUtc && transfer.InventoryTransferId < boundary.Id))
                : query.Where(transfer => transfer.CreatedAtUtc > boundary.CreatedAtUtc || (transfer.CreatedAtUtc == boundary.CreatedAtUtc && transfer.InventoryTransferId > boundary.Id));
        }

        var ordered = boundary?.Backwards == true
            ? query.OrderByDescending(transfer => transfer.CreatedAtUtc).ThenByDescending(transfer => transfer.InventoryTransferId)
            : query.OrderBy(transfer => transfer.CreatedAtUtc).ThenBy(transfer => transfer.InventoryTransferId);


        var transfers = await ordered.Take(pageSize + 1)
            .Select(transfer => new CursorRow<InventoryTransferResponse>(
                transfer.CreatedAtUtc, transfer.InventoryTransferId,
                new InventoryTransferResponse(
                    transfer.InventoryTransferPublicId, transfer.TransferStatusCode,
                    transfer.SourceLocationId, transfer.DestinationLocationId,
                    transfer.CreatedAtUtc, transfer.UpdatedAtUtc,transfer.RowVersion,
                    new TenantResponse(
                        transfer.Tenant.TenantId, transfer.Tenant.TenantPublicId,
                        transfer.Tenant.TenantName, transfer.Tenant.IsActive,
                        transfer.Tenant.CreatedAtUtc, transfer.Tenant.UpdatedAtUtc))))
            .ToListAsync(cancellationToken);

        return Ok(CursorPage.Create(transfers, pageSize, boundary, context));
    }

    /// <summary>
    /// Retrieves one transfer header, its source and destination, and all its lines.
    /// Includes product, lot, unit, and current stock at the exact source location.
    /// An existing transfer with no lines is returned with an empty line collection.
    /// </summary>
    /// <returns>The transfer detail, or 404 when no header matches both identifiers.</returns>
    [HttpGet("{tenantId:int}/{inventoryTransferPublicId:guid}")]
    public async Task<ActionResult<InventoryTransferDetailResponse>>
        GetInventoryTransfer(int tenantId, Guid inventoryTransferPublicId, CancellationToken cancellationToken)
    {
        var detail = await dbContext.InventoryTransfers
            .AsNoTracking()
            .AsSingleQuery()
            .Where(transfer => transfer.TenantId == tenantId && transfer.InventoryTransferPublicId == inventoryTransferPublicId)
            .Select(transfer => new InventoryTransferDetailResponse(
                new InventoryTransferResponse(
                    transfer.InventoryTransferPublicId, transfer.TransferStatusCode,
                    transfer.SourceLocationId, transfer.DestinationLocationId,
                    transfer.CreatedAtUtc, transfer.UpdatedAtUtc, transfer.RowVersion,
                    new TenantResponse(
                        transfer.Tenant.TenantId, transfer.Tenant.TenantPublicId, transfer.Tenant.TenantName,
                        transfer.Tenant.IsActive, transfer.Tenant.CreatedAtUtc, transfer.Tenant.UpdatedAtUtc)),
                new InventoryTransferLocationResponse(
                    transfer.WarehouseLocationNavigation.Warehouse.WarehousePublicId,
                    transfer.WarehouseLocationNavigation.Warehouse.WarehouseCode,
                    transfer.WarehouseLocationNavigation.Warehouse.WarehouseName,
                    transfer.WarehouseLocationNavigation.Warehouse.IsActive,
                    transfer.WarehouseLocationNavigation.WarehouseLocationId,
                    transfer.WarehouseLocationNavigation.LocationCode,
                    transfer.WarehouseLocationNavigation.DisplayName,
                    transfer.WarehouseLocationNavigation.IsActive),
                new InventoryTransferLocationResponse(
                    transfer.WarehouseLocation.Warehouse.WarehousePublicId,
                    transfer.WarehouseLocation.Warehouse.WarehouseCode,
                    transfer.WarehouseLocation.Warehouse.WarehouseName,
                    transfer.WarehouseLocation.Warehouse.IsActive,
                    transfer.WarehouseLocation.WarehouseLocationId,
                    transfer.WarehouseLocation.LocationCode,
                    transfer.WarehouseLocation.DisplayName,
                    transfer.WarehouseLocation.IsActive),
                transfer.InventoryTransferLines
                    .OrderBy(line => line.CreatedAtUtc)
                    .ThenBy(line => line.InventoryTransferLineId)
                    .Select(line => new InventoryTransferDetailLineResponse(
                        line.InventoryTransferLineId, line.InventoryLotId, line.QuantityRequested, line.CreatedAtUtc,
                        line.InventoryLot.Product.ProductPublicId, line.InventoryLot.Product.Sku,
                        line.InventoryLot.Product.Name, line.InventoryLot.Product.IsActive,
                        line.InventoryLot.LotNumber, line.InventoryLot.ExpirationDate,
                        line.InventoryLot.Product.UnitOfMeasure.UnitOfMeasureCode,
                        line.InventoryLot.InventoryBalances
                            .Where(balance =>
                                balance.TenantId == transfer.TenantId &&
                                balance.WarehouseLocationId == transfer.SourceLocationId)
                            .Select(balance => new InventoryTransferSourceStockResponse(
                                balance.QuantityOnHand, balance.QuantityReserved, balance.QuantityAvailable))
                            .FirstOrDefault()))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        if (detail is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: "The transfer was not found.");
           
        return Ok(detail);
    }

}

