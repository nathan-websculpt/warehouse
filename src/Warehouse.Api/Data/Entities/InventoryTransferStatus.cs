using System;
using System.Collections.Generic;

namespace Warehouse.Api.Data.Entities;

public partial class InventoryTransferStatus
{
    public string TransferStatusCode { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public bool IsTerminal { get; set; }

    public virtual ICollection<InventoryTransfer> InventoryTransfers { get; set; } = new List<InventoryTransfer>();
}
