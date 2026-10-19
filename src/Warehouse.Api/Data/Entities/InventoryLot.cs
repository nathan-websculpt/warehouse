using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class InventoryLot
{
    public int InventoryLotId { get; set; }

    public int TenantId { get; set; }

    public int ProductId { get; set; }

    public string LotNumber { get; set; } = null!;

    public DateOnly? ManufactureDate { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    public string? SupplierLotReference { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();

    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();

    public virtual ICollection<InventoryTransferLine> InventoryTransferLines { get; set; } = new List<InventoryTransferLine>();

    public virtual Product Product { get; set; } = null!;
}
