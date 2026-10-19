using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data.Entities;

namespace Warehouse.Api.Data;

public partial class WarehouseDbContext
{
    // Keep handwritten configuration outside files replaced by database scaffolding.
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryTransfer>().ToTable(table => table.UseSqlOutputClause(false));
        modelBuilder.Entity<InventoryTransferLine>().ToTable(table => table.UseSqlOutputClause(false));
    }
}
