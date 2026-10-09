using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public int TenantId { get; set; }

    public Guid WarehousePublicId { get; set; }

    public string WarehouseCode { get; set; } = null!;

    public string WarehouseName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;
}
