using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class WarehouseContact
{
    public int WarehouseContactId { get; set; }

    public int TenantId { get; set; }

    public int WarehouseId { get; set; }

    public string ContactName { get; set; } = null!;

    public string ContactRole { get; set; } = null!;

    public string PrimaryPhoneNumber { get; set; } = null!;

    public string? PrimaryPhoneExtension { get; set; }

    public string? SecondaryPhoneNumber { get; set; }

    public string? SecondaryPhoneExtension { get; set; }

    public string EmailAddress { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}
