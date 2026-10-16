using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Warehouse.Api.Data.Queries;
using Warehouse.Contracts.InventoryTransferLines;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/transfer-lines/{tenantId:int}/{inventoryTransferPublicId:guid}/finalize-transfer")]
public sealed class FinalizeTransferController(
    CompleteInventoryTransferQuery completeInventoryTransferQuery,
    ILogger<FinalizeTransferController> logger,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CompleteInventoryTransferResponse>> FinalizeTransfer(
        [FromRoute] int tenantId,
        [FromRoute] Guid inventoryTransferPublicId,
        [FromBody] CompleteInventoryTransferRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: need tenant authorization/access check
            var transferResponse = await completeInventoryTransferQuery.ExecuteAsync(tenantId, inventoryTransferPublicId, request.ExpectedRowVersion, cancellationToken);
            
            logger.LogInformation(
                "Transfer {TransferId} for tenant {TenantId} completed: " +
                "status={Status}, replay={IsReplay}, lines={LineCount}.",
                inventoryTransferPublicId,
                tenantId,
                transferResponse.Summary.TransferStatusCode,
                transferResponse.Summary.IsReplay,
                transferResponse.Summary.LineCount);

            return Ok(transferResponse);
        }
        catch (SqlException exception) 
        {
            var statusCode = exception.Number switch
            {
                51000 => StatusCodes.Status400BadRequest,
                51001 => StatusCodes.Status404NotFound,
                51002 or 51003 => StatusCodes.Status409Conflict,
                51004 or 51005 or 51006 or 51007 or 51008
                    or 51009 or 51010 or 51011 or 51013 => StatusCodes.Status422UnprocessableEntity,
                1205 => StatusCodes.Status503ServiceUnavailable,                
                _ => StatusCodes.Status500InternalServerError // includes 51012, 51014, 50000, and unexpected SQL errors.
            };

            logger.Log(
                statusCode >= 500 ? LogLevel.Error : LogLevel.Warning,
                exception,
                "Transfer {TransferId} failed with SQL error {SqlErrorNumber}.",
                inventoryTransferPublicId,
                exception.Number);

            string detail;

            if (statusCode < 500 || environment.IsDevelopment()) {
                detail = exception.Message;
            }
            else if (exception.Number == 1205) {
                detail = "A temporary database conflict occurred. Please retry completion.";
            }
            else {
                detail = "A database error occurred while completing the transfer.";
            }

            return Problem(statusCode: statusCode, title: "Transfer cannot be completed.", detail: detail);
        }
    }
}
