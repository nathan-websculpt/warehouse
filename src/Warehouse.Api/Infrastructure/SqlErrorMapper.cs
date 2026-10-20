using Warehouse.Contracts.Errors;

namespace Warehouse.Api.Infrastructure;

public enum SqlOperation { Other, Transfer, StockAvailability }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SqlOperationAttribute(SqlOperation operation) : Attribute
{
    public SqlOperation Operation { get; } = operation;
}

public sealed record SqlFailure(int Status, string Code, string Title);

public static class SqlErrorMapper
{
    public static SqlFailure Map(SqlOperation operation, int number) => (operation, number) switch
    {
        (_, 1205 or -2) => new(503, "DATABASE_TEMPORARILY_UNAVAILABLE", "A temporary database failure occurred. Refresh before retrying."),
        (_, 51016) => new(409, ErrorCodes.Immutable, "Approved and received transfer contents cannot be changed."),
        (SqlOperation.Transfer, 51000) => new(400, ErrorCodes.Validation, "Invalid completion request."),
        (SqlOperation.Transfer, 51001) => new(404, "TRANSFER_NOT_FOUND", "Transfer not found for this tenant."),
        (SqlOperation.Transfer, 51002) => new(409, ErrorCodes.StatusConflict, "Transfer must be approved before completion."),
        (SqlOperation.Transfer, 51003) => new(409, ErrorCodes.VersionConflict, "The transfer changed. Refresh and review it."),
        (SqlOperation.Transfer, 51004) => new(422, "TRANSFER_TENANT_INACTIVE", "Tenant must be active."),
        (SqlOperation.Transfer, 51005) => new(422, "TRANSFER_ROUTE_INVALID", "The transfer route is invalid."),  
        (SqlOperation.Transfer, 51006) => new(422, "TRANSFER_WAREHOUSE_INACTIVE", "Both warehouses must be active."),
        (SqlOperation.Transfer, 51007) => new(422, "TRANSFER_LOCATION_INACTIVE", "Both locations must be active."),
        (SqlOperation.Transfer, 51008) => new(422, "TRANSFER_HAS_NO_LINES", "Transfer must contain at least one line."),
        (SqlOperation.Transfer, 51009) => new(422, "TRANSFER_PRODUCT_INACTIVE", "The product must be active."),
        (SqlOperation.Transfer, 51010) => new(422, "TRANSFER_LOT_SHELF_LIFE", "The lot must have at least seven UTC calendar days of shelf life."),
        (SqlOperation.Transfer, 51011) => new(422, "TRANSFER_SOURCE_BALANCE_MISSING", "The exact source lot has no balance."),
        (SqlOperation.Transfer, 51013) => new(422, "TRANSFER_INSUFFICIENT_STOCK", "Insufficient unreserved source stock."),
        (SqlOperation.Transfer, 51015) => new(409, ErrorCodes.CompletionUnavailable, "This transfer has no completed receipt to retrieve."),
        
        // includes 51012, 51014, 50000, and unexpected SQL errors
        // history failures have their own code -- 50000 and other unexpected SQL errors use the fallback below
        (SqlOperation.Transfer, 51012 or 51014) => new(500, "TRANSFER_HISTORY_INCONSISTENT", "Transfer history could not be verified. Contact support with the reference."),
        (SqlOperation.StockAvailability, 51000 or 51001 or 51002 or 51003) => new(400, ErrorCodes.Validation, "Invalid stock report request."),
        (SqlOperation.StockAvailability, 51004 or 51006) => new(404, ErrorCodes.NotFound, "Tenant or warehouse was not found."),
        (SqlOperation.StockAvailability, 51005 or 51007) => new(422, "STOCK_SCOPE_INACTIVE", "Tenant and warehouse must be active."),
        _ => new(500, ErrorCodes.Unexpected, "An unexpected database error occurred. Contact support with the reference.")
    };
}
