using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class Tenant
{
    public int TenantId { get; set; }

    public Guid TenantPublicId { get; set; }

    public string TenantName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual ICollection<InventoryTransferLine> InventoryTransferLines { get; set; } = new List<InventoryTransferLine>();

    public virtual ICollection<InventoryTransfer> InventoryTransfers { get; set; } = new List<InventoryTransfer>();

    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
