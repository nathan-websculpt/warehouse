using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class WarehouseAddress
{
    public int WarehouseAddressId { get; set; }

    public int TenantId { get; set; }

    public int WarehouseId { get; set; }

    public string AddressLine1 { get; set; } = null!;

    public string? AddressLine2 { get; set; }

    public string City { get; set; } = null!;

    public string StateCode { get; set; } = null!;

    public string ZipCode { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual State StateCodeNavigation { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
