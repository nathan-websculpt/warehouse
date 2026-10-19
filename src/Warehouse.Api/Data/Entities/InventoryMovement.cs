using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class InventoryMovement
{
    public int InventoryMovementId { get; set; }

    public int TenantId { get; set; }

    public int InventoryTransferLineId { get; set; }

    public int InventoryLotId { get; set; }

    public decimal Quantity { get; set; }

    public int SourceLocationId { get; set; }

    public int DestinationLocationId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual InventoryLot InventoryLot { get; set; } = null!;

    public virtual InventoryTransferLine InventoryTransferLine { get; set; } = null!;

    public virtual WarehouseLocation WarehouseLocation { get; set; } = null!;

    public virtual WarehouseLocation WarehouseLocationNavigation { get; set; } = null!;
}
