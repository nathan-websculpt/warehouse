namespace Warehouse.Api.Contracts.Tenants;

public sealed record TenantResponse(
    Guid TenantPublicId,
    string TenantName,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

