using Microsoft.Data.SqlClient;
using System.Data;
using Warehouse.Contracts.InventoryTransferLines;

namespace Warehouse.Api.Data.Queries;

// executes dbo.CompleteInventoryTransfer
//      sproc returns 2 result sets
public sealed class CompleteInventoryTransferQuery(SqlConnectionFactory connectionFactory)
{
    public async Task<CompleteInventoryTransferResponse> ExecuteAsync(
        int tenantId, 
        Guid inventoryTransferPublicId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expectedRowVersion);
        if (expectedRowVersion.Length != 8)
            throw new ArgumentException("ExpectedRowVersion must be exactly 8 bytes.", nameof(expectedRowVersion));

        return await ReadAsync(tenantId, inventoryTransferPublicId, expectedRowVersion, false, cancellationToken);
    }

    public Task<CompleteInventoryTransferResponse> RetrieveAsync(int tenantId, Guid inventoryTransferPublicId, CancellationToken cancellationToken) =>
        ReadAsync(tenantId, inventoryTransferPublicId, null, true, cancellationToken);

    private async Task<CompleteInventoryTransferResponse> ReadAsync(
        int tenantId,
        Guid inventoryTransferPublicId,
        byte[]? expectedRowVersion,
        bool retrieveOnly,
        CancellationToken cancellationToken)
    {

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.CompleteInventoryTransfer", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter("@TenantId", SqlDbType.Int)
            {
                Value = tenantId
            });

        command.Parameters.Add(
            new SqlParameter("@InventoryTransferPublicId", SqlDbType.UniqueIdentifier)
            {
                Value = inventoryTransferPublicId
            });

        command.Parameters.Add(
            new SqlParameter("@ExpectedRowVersion", SqlDbType.Binary, 8)
            {
                Value = (object?)expectedRowVersion ?? DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter("@RetrieveOnly", SqlDbType.Bit) 
            {  
                Value = retrieveOnly
            });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // result set 1: Transfer Summary
        var transferPublicIdOrdinal = reader.GetOrdinal("InventoryTransferPublicId");
        var statusOrdinal = reader.GetOrdinal("TransferStatusCode");
        var completedAtOrdinal = reader.GetOrdinal("CompletedAtUtc");
        var rowVersionOrdinal = reader.GetOrdinal("RowVersion");
        var lineCountOrdinal = reader.GetOrdinal("LineCount");
        var isReplayOrdinal = reader.GetOrdinal("IsReplay");

        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Expected a Transfer Summary.");

        var summary = new InventoryTransferCompletionSummaryResponse(
            reader.GetGuid(transferPublicIdOrdinal),
            reader.GetString(statusOrdinal),
            DateTime.SpecifyKind(reader.GetDateTime(completedAtOrdinal), DateTimeKind.Utc),
            reader.GetFieldValue<byte[]>(rowVersionOrdinal),
            reader.GetInt32(lineCountOrdinal),
            reader.GetBoolean(isReplayOrdinal));



        // result set 2: Movements

        if (!await reader.NextResultAsync(cancellationToken))
            throw new InvalidOperationException("Expected the movement result set.");

        var inventoryTransferLineIdOrdinal = reader.GetOrdinal("InventoryTransferLineId");
        var skuOrdinal = reader.GetOrdinal("SKU");
        var lotNumberOrdinal = reader.GetOrdinal("LotNumber");
        var unitOfMeasureOrdinal = reader.GetOrdinal("UnitOfMeasureCode");
        var quantityMovedOrdinal = reader.GetOrdinal("QuantityMoved");
        var inventoryMovementIdOrdinal = reader.GetOrdinal("InventoryMovementId");
        var movementCreatedAtUtcOrdinal = reader.GetOrdinal("MovementCreatedAtUtc");

        var movements = new List<InventoryTransferMovementResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var movement = new InventoryTransferMovementResponse(
                reader.GetInt32(inventoryTransferLineIdOrdinal),
                reader.GetString(skuOrdinal),
                reader.GetString(lotNumberOrdinal), 
                reader.GetString(unitOfMeasureOrdinal),
                reader.GetDecimal(quantityMovedOrdinal),
                reader.GetInt32(inventoryMovementIdOrdinal),
                DateTime.SpecifyKind(reader.GetDateTime(movementCreatedAtUtcOrdinal), DateTimeKind.Utc));
            
            movements.Add(movement);
        }

        if (movements.Count != summary.LineCount)
            throw new InvalidOperationException("Movement count does not match the transfer summary.");


        return new CompleteInventoryTransferResponse(summary, movements);
    }
}