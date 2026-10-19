using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class Product
{
    public int ProductId { get; set; }

    public Guid ProductPublicId { get; set; }

    public int TenantId { get; set; }

    public string Sku { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int UnitOfMeasureId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual ICollection<InventoryLot> InventoryLots { get; set; } = new List<InventoryLot>();

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual UnitOfMeasure UnitOfMeasure { get; set; } = null!;
}
