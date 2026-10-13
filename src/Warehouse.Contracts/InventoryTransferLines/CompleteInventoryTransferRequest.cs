using System.ComponentModel.DataAnnotations;

namespace Warehouse.Contracts.InventoryTransferLines;

public sealed class CompleteInventoryTransferRequest
{
    [Required]
    [MinLength(8)]
    [MaxLength(8)]
    public byte[] ExpectedRowVersion { get; init; } = [];
}
