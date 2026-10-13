using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Contracts.Tenants;

namespace Warehouse.Contracts.InventoryTransfers;

public sealed record InventoryTransferResponse(
    Guid InventoryTransferPublicId,
    string TransferStatusCode,
    int SourceLocationId,
    int DestinationLocationId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    byte[] RowVersion,
    TenantResponse Tenant
    );


