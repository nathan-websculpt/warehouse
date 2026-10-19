using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class InventoryTransferLine
{
    public int InventoryTransferLineId { get; set; }

    public int TenantId { get; set; }

    public int InventoryTransferId { get; set; }

    public int InventoryLotId { get; set; }

    public decimal QuantityRequested { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual InventoryLot InventoryLot { get; set; } = null!;

    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();

    public virtual InventoryTransfer InventoryTransfer { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;
}
