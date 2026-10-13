using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Contracts.InventoryTransfers;

namespace Warehouse.Contracts.InventoryTransferLines;

public sealed record InventoryTransferLineResponse(
    DateTime CreatedAtUtc,
    decimal QuantityRequested,
    InventoryTransferResponse InventoryTransfer);
