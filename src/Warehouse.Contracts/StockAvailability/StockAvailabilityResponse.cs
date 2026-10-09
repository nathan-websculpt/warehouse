namespace Warehouse.Contracts.StockAvailability;

public sealed record StockAvailabilityResponse(
    Guid ProductPublicId,
    string SKU,
    string ProductName,
    string UnitOfMeasureCode,
    DateOnly ReportDateUtc,
    decimal PhysicalOnHand,
    decimal EligibleOnHand,
    decimal EligibleReserved,
    decimal AvailableToPick,
    decimal ExpiredOnHand,
    decimal InactiveLocationOnHand,
    int EligibleLotCount,
    string StockStatus);