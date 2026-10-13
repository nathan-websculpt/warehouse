namespace Warehouse.Contracts.Tenants;

public sealed record TenantResponse(
    int TenantId,
    Guid TenantPublicId,
    string TenantName,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);