namespace Warehouse.Contracts.Tenants;

public sealed record TenantResponse(
    Guid TenantPublicId,
    string TenantName,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);