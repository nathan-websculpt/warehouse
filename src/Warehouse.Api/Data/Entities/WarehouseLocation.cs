using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class WarehouseLocation
{
    public int WarehouseLocationId { get; set; }

    public int WarehouseId { get; set; }

    public int TenantId { get; set; }

    public string? DisplayName { get; set; }

    public string LocationCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();

    public virtual ICollection<InventoryMovement> InventoryMovementWarehouseLocationNavigations { get; set; } = new List<InventoryMovement>();

    public virtual ICollection<InventoryMovement> InventoryMovementWarehouseLocations { get; set; } = new List<InventoryMovement>();

    public virtual ICollection<InventoryTransfer> InventoryTransferWarehouseLocationNavigations { get; set; } = new List<InventoryTransfer>();

    public virtual ICollection<InventoryTransfer> InventoryTransferWarehouseLocations { get; set; } = new List<InventoryTransfer>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
