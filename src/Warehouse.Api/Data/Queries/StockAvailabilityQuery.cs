using Microsoft.Data.SqlClient;
using System.Data;
using Warehouse.Api.Contracts.StockAvailability;

namespace Warehouse.Api.Data.Queries;

public sealed class StockAvailabilityQuery(SqlConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<StockAvailabilityResponse>> ExecuteAsync(
        int tenantId,
        Guid warehousePublicId,
        decimal lowStockThreshold,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(
                "dbo.GetWarehouseStockAvailability",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

        command.Parameters.Add(
            new SqlParameter("@TenantId", SqlDbType.Int)
            {
                Value = tenantId
            });

        command.Parameters.Add(
            new SqlParameter(
                "@WarehousePublicId",
                SqlDbType.UniqueIdentifier)
            {
                Value = warehousePublicId
            });

        command.Parameters.Add(
            new SqlParameter(
                "@LowStockThreshold",
                SqlDbType.Decimal)
            {
                Precision = 19,
                Scale = 4,
                Value = lowStockThreshold
            });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var productPublicIdOrdinal = reader.GetOrdinal("ProductPublicId");
        var skuOrdinal = reader.GetOrdinal("SKU");
        var productNameOrdinal = reader.GetOrdinal("ProductName");
        var unitOfMeasureCodeOrdinal = reader.GetOrdinal("UnitOfMeasureCode");
        var reportDateUtcOrdinal = reader.GetOrdinal("ReportDateUtc");
        var physicalOnHandOrdinal = reader.GetOrdinal("PhysicalOnHand");
        var eligibleOnHandOrdinal = reader.GetOrdinal("EligibleOnHand");
        var eligibleReservedOrdinal = reader.GetOrdinal("EligibleReserved");
        var availableToPickOrdinal = reader.GetOrdinal("AvailableToPick");
        var expiredOnHandOrdinal = reader.GetOrdinal("ExpiredOnHand");
        var inactiveLocationOnHandOrdinal = reader.GetOrdinal("InactiveLocationOnHand");
        var eligibleLotCountOrdinal = reader.GetOrdinal("EligibleLotCount");
        var stockStatusOrdinal = reader.GetOrdinal("StockStatus");

        var results = new List<StockAvailabilityResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new StockAvailabilityResponse(
                    reader.GetGuid(productPublicIdOrdinal),
                    reader.GetString(skuOrdinal),
                    reader.GetString(productNameOrdinal),
                    reader.GetString(unitOfMeasureCodeOrdinal),
                    reader.GetFieldValue<DateOnly>(reportDateUtcOrdinal),
                    reader.GetDecimal(physicalOnHandOrdinal),
                    reader.GetDecimal(eligibleOnHandOrdinal),
                    reader.GetDecimal(eligibleReservedOrdinal),
                    reader.GetDecimal(availableToPickOrdinal),
                    reader.GetDecimal(expiredOnHandOrdinal),
                    reader.GetDecimal(inactiveLocationOnHandOrdinal),
                    reader.GetInt32(eligibleLotCountOrdinal),
                    reader.GetString(stockStatusOrdinal)));
        }

        return results;
    }
}
