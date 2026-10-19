using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class UnitOfMeasure
{
    public int UnitOfMeasureId { get; set; }

    public string UnitOfMeasureCode { get; set; } = null!;

    public string UnitOfMeasureName { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
