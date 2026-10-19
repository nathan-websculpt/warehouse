using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class State
{
    public string StateCode { get; set; } = null!;

    public string StateName { get; set; } = null!;

    public virtual ICollection<WarehouseAddress> WarehouseAddresses { get; set; } = new List<WarehouseAddress>();
}
