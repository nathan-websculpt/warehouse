using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Data.Queries;
using Warehouse.Api.Infrastructure;
using Warehouse.Contracts.InventoryTransferLines;

namespace Warehouse.Api.Controllers;

[ApiController]
[SqlOperation(SqlOperation.Transfer)]
public sealed class FinalizeTransferController(CompleteInventoryTransferQuery query, ILogger<FinalizeTransferController> logger) : ControllerBase
{
    // tenant ownership is enforced by SQL
    [HttpPost("api/transfer-lines/{tenantId:int}/{inventoryTransferPublicId:guid}/finalize-transfer")]
    public async Task<ActionResult<CompleteInventoryTransferResponse>> 
        FinalizeTransfer(
            int tenantId,
            Guid inventoryTransferPublicId,
            [FromBody] CompleteInventoryTransferRequest request,
            CancellationToken cancellationToken)
    {
        // TODO: need tenant authorization/access check... unless this acts as "Tech Support" app with global access -- haven't decided
        var result = await query.ExecuteAsync(tenantId, inventoryTransferPublicId, request.ExpectedRowVersion, cancellationToken);

        logger.LogInformation("Transfer {TransferId} for tenant {TenantId}: status={Status}, replay={IsReplay}, lines={LineCount}.",
            inventoryTransferPublicId, tenantId, result.Summary.TransferStatusCode, result.Summary.IsReplay, result.Summary.LineCount);


        return Ok(result);
    }
     
    /// <summary>Retrieves verified completion history without posting stock or changing the transfer.</summary>
    [HttpGet("api/transfers/{tenantId:int}/{inventoryTransferPublicId:guid}/completion")]
    public async Task<ActionResult<CompleteInventoryTransferResponse>> 
        RetrieveCompletion(int tenantId, Guid inventoryTransferPublicId, CancellationToken cancellationToken) =>
            Ok(await query.RetrieveAsync(tenantId, inventoryTransferPublicId, cancellationToken));
}
