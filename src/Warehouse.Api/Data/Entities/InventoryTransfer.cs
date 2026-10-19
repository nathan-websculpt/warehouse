using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class InventoryTransfer
{
    public int InventoryTransferId { get; set; }

    public Guid InventoryTransferPublicId { get; set; }

    public int TenantId { get; set; }

    public string TransferStatusCode { get; set; } = null!;

    public int SourceLocationId { get; set; }

    public int DestinationLocationId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<InventoryTransferLine> InventoryTransferLines { get; set; } = new List<InventoryTransferLine>();

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual InventoryTransferStatus TransferStatusCodeNavigation { get; set; } = null!;

    public virtual WarehouseLocation WarehouseLocation { get; set; } = null!;

    public virtual WarehouseLocation WarehouseLocationNavigation { get; set; } = null!;
}
