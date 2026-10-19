using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class InventoryBalance
{
    public int InventoryBalanceId { get; set; }

    public int TenantId { get; set; }

    public int WarehouseLocationId { get; set; }

    public int InventoryLotId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityReserved { get; set; }

    public decimal? QuantityAvailable { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual InventoryLot InventoryLot { get; set; } = null!;

    public virtual WarehouseLocation WarehouseLocation { get; set; } = null!;
}
