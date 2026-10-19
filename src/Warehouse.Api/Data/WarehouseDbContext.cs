using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data.Entities;

namespace Warehouse.Api.Data;

using Warehouse = global::Warehouse.Api.Data.Entities.Warehouse;

public partial class WarehouseDbContext : DbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<InventoryBalance> InventoryBalances { get; set; }

    public virtual DbSet<InventoryLot> InventoryLots { get; set; }

    public virtual DbSet<InventoryMovement> InventoryMovements { get; set; }

    public virtual DbSet<InventoryTransfer> InventoryTransfers { get; set; }

    public virtual DbSet<InventoryTransferLine> InventoryTransferLines { get; set; }

    public virtual DbSet<InventoryTransferStatus> InventoryTransferStatuses { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<State> States { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<UnitOfMeasure> UnitOfMeasures { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WarehouseAddress> WarehouseAddresses { get; set; }

    public virtual DbSet<WarehouseContact> WarehouseContacts { get; set; }

    public virtual DbSet<WarehouseLocation> WarehouseLocations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.WarehouseLocationId, e.InventoryLotId }, "UQ_InventoryBalances_TenantId_WarehouseLocationId_InventoryLotId").IsUnique();

            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_InventoryBalances_CreatedAtUtc");
            entity.Property(e => e.QuantityAvailable)
                .HasComputedColumnSql("([QuantityOnHand]-[QuantityReserved])", false)
                .HasColumnType("decimal(20, 4)");
            entity.Property(e => e.QuantityOnHand).HasColumnType("decimal(19, 4)");
            entity.Property(e => e.QuantityReserved).HasColumnType("decimal(19, 4)");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.InventoryLot).WithMany(p => p.InventoryBalances)
                .HasPrincipalKey(p => new { p.TenantId, p.InventoryLotId })
                .HasForeignKey(d => new { d.TenantId, d.InventoryLotId })
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WarehouseLocation).WithMany(p => p.InventoryBalances)
                .HasPrincipalKey(p => new { p.TenantId, p.WarehouseLocationId })
                .HasForeignKey(d => new { d.TenantId, d.WarehouseLocationId })
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<InventoryLot>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.InventoryLotId }, "UQ_InventoryLots_TenantId_InventoryLotId").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.ProductId, e.LotNumber }, "UQ_InventoryLots_TenantId_ProductId_LotNumber").IsUnique();

            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_InventoryLots_CreatedAtUtc");
            entity.Property(e => e.LotNumber).HasMaxLength(100);
            entity.Property(e => e.SupplierLotReference).HasMaxLength(50);

            entity.HasOne(d => d.Product).WithMany(p => p.InventoryLots)
                .HasPrincipalKey(p => new { p.TenantId, p.ProductId })
                .HasForeignKey(d => new { d.TenantId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_InventoryMovements_CreatedAtUtc");
            entity.Property(e => e.Quantity).HasColumnType("decimal(19, 4)");

            entity.HasOne(d => d.WarehouseLocation).WithMany(p => p.InventoryMovementWarehouseLocations)
                .HasPrincipalKey(p => new { p.TenantId, p.WarehouseLocationId })
                .HasForeignKey(d => new { d.TenantId, d.DestinationLocationId })
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.InventoryLot).WithMany(p => p.InventoryMovements)
                .HasPrincipalKey(p => new { p.TenantId, p.InventoryLotId })
                .HasForeignKey(d => new { d.TenantId, d.InventoryLotId })
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WarehouseLocationNavigation).WithMany(p => p.InventoryMovementWarehouseLocationNavigations)
                .HasPrincipalKey(p => new { p.TenantId, p.WarehouseLocationId })
                .HasForeignKey(d => new { d.TenantId, d.SourceLocationId })
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.InventoryTransferLine).WithMany(p => p.InventoryMovements)
                .HasPrincipalKey(p => new { p.TenantId, p.InventoryTransferLineId, p.InventoryLotId })
                .HasForeignKey(d => new { d.TenantId, d.InventoryTransferLineId, d.InventoryLotId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryMovements_InventoryTransferLines_Tenant_Line_Lot");
        });

        modelBuilder.Entity<InventoryTransfer>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("TR_InventoryTransfers_ProtectContent"));

            entity.HasIndex(e => new { e.CreatedAtUtc, e.InventoryTransferId }, "IX_InventoryTransfers_Page");

            entity.HasIndex(e => new { e.TenantId, e.CreatedAtUtc, e.InventoryTransferId }, "IX_InventoryTransfers_TenantPage");

            entity.HasIndex(e => e.InventoryTransferPublicId, "UQ_InventoryTransfers_InventoryTransferPublicId").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.InventoryTransferId }, "UQ_InventoryTransfers_TenantId_InventoryTransferId").IsUnique();

            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_InventoryTransfers_CreatedAtUtc");
            entity.Property(e => e.InventoryTransferPublicId).HasDefaultValueSql("(newid())", "DF_InventoryTransfers_InventoryTransferPublicId");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TransferStatusCode)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Tenant).WithMany(p => p.InventoryTransfers)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TransferStatusCodeNavigation).WithMany(p => p.InventoryTransfers)
                .HasForeignKey(d => d.TransferStatusCode)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WarehouseLocation).WithMany(p => p.InventoryTransferWarehouseLocations)
                .HasPrincipalKey(p => new { p.TenantId, p.WarehouseLocationId })
                .HasForeignKey(d => new { d.TenantId, d.DestinationLocationId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryTransfers_WarehouseLocations_DestinationLocationId");

            entity.HasOne(d => d.WarehouseLocationNavigation).WithMany(p => p.InventoryTransferWarehouseLocationNavigations)
                .HasPrincipalKey(p => new { p.TenantId, p.WarehouseLocationId })
                .HasForeignKey(d => new { d.TenantId, d.SourceLocationId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryTransfers_WarehouseLocations_SourceLocationId");
        });

        modelBuilder.Entity<InventoryTransferLine>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("TR_InventoryTransferLines_ProtectContent"));

            entity.HasIndex(e => new { e.CreatedAtUtc, e.InventoryTransferLineId }, "IX_InventoryTransferLines_Page");

            entity.HasIndex(e => new { e.TenantId, e.CreatedAtUtc, e.InventoryTransferLineId }, "IX_InventoryTransferLines_TenantPage");

            entity.HasIndex(e => new { e.TenantId, e.InventoryTransferId, e.InventoryLotId }, "UQ_InventoryTransferLines_TenantId_InventoryTransferId_InventoryLotId").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.InventoryTransferLineId, e.InventoryLotId }, "UQ_InventoryTransferLines_TenantId_InventoryTransferLineId_InventoryLotId").IsUnique();

            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_InventoryTransferLines_CreatedAtUtc");
            entity.Property(e => e.QuantityRequested).HasColumnType("decimal(19, 4)");

            entity.HasOne(d => d.Tenant).WithMany(p => p.InventoryTransferLines)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.InventoryLot).WithMany(p => p.InventoryTransferLines)
                .HasPrincipalKey(p => new { p.TenantId, p.InventoryLotId })
                .HasForeignKey(d => new { d.TenantId, d.InventoryLotId })
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.InventoryTransfer).WithMany(p => p.InventoryTransferLines)
                .HasPrincipalKey(p => new { p.TenantId, p.InventoryTransferId })
                .HasForeignKey(d => new { d.TenantId, d.InventoryTransferId })
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<InventoryTransferStatus>(entity =>
        {
            entity.HasKey(e => e.TransferStatusCode);

            entity.Property(e => e.TransferStatusCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DisplayName).HasMaxLength(100);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.ProductPublicId, "UQ_Products_ProductPublicId").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.ProductId }, "UQ_Products_TenantId_ProductId").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Sku }, "UQ_Products_TenantId_SKU").IsUnique();

            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_Products_CreatedAtUtc");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Products_IsActive");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.ProductPublicId).HasDefaultValueSql("(newid())", "DF_Products_ProductPublicId");
            entity.Property(e => e.Sku)
                .HasMaxLength(100)
                .HasColumnName("SKU");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Products)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.UnitOfMeasure).WithMany(p => p.Products)
                .HasForeignKey(d => d.UnitOfMeasureId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => e.StateCode);

            entity.HasIndex(e => e.StateName, "UQ_States_StateName").IsUnique();

            entity.Property(e => e.StateCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.StateName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.TenantPublicId, "UQ_Tenants_TenantPublicId").IsUnique();

            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_Tenants_CreatedAtUtc");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Tenants_IsActive");
            entity.Property(e => e.TenantName).HasMaxLength(200);
            entity.Property(e => e.TenantPublicId).HasDefaultValueSql("(newid())", "DF_Tenants_TenantPublicId");
        });

        modelBuilder.Entity<UnitOfMeasure>(entity =>
        {
            entity.ToTable("UnitOfMeasure");

            entity.HasIndex(e => e.UnitOfMeasureCode, "UQ_UnitOfMeasure_UnitOfMeasureCode").IsUnique();

            entity.HasIndex(e => e.UnitOfMeasureName, "UQ_UnitOfMeasure_UnitOfMeasureName").IsUnique();

            entity.Property(e => e.UnitOfMeasureCode).HasMaxLength(3);
            entity.Property(e => e.UnitOfMeasureName).HasMaxLength(20);
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.WarehouseCode }, "UQ_Warehouses_TenantId_WarehouseCode").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.WarehouseId }, "UQ_Warehouses_TenantId_WarehouseId").IsUnique();

            entity.HasIndex(e => e.WarehousePublicId, "UQ_Warehouses_WarehousePublicId").IsUnique();

            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_Warehouses_CreatedAtUtc");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Warehouses_IsActive");
            entity.Property(e => e.WarehouseCode).HasMaxLength(50);
            entity.Property(e => e.WarehouseName).HasMaxLength(200);
            entity.Property(e => e.WarehousePublicId).HasDefaultValueSql("(newid())", "DF_Warehouses_WarehousePublicId");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Warehouses)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WarehouseAddress>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.WarehouseId }, "UQ_WarehouseAddresses_TenantId_WarehouseId").IsUnique();

            entity.Property(e => e.AddressLine1).HasMaxLength(100);
            entity.Property(e => e.AddressLine2).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_WarehouseAddresses_CreatedAtUtc");
            entity.Property(e => e.StateCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.ZipCode)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.StateCodeNavigation).WithMany(p => p.WarehouseAddresses)
                .HasForeignKey(d => d.StateCode)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Warehouse).WithOne(p => p.WarehouseAddress)
                .HasPrincipalKey<Warehouse>(p => new { p.TenantId, p.WarehouseId })
                .HasForeignKey<WarehouseAddress>(d => new { d.TenantId, d.WarehouseId })
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WarehouseContact>(entity =>
        {
            entity.Property(e => e.ContactName).HasMaxLength(100);
            entity.Property(e => e.ContactRole).HasMaxLength(100);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_WarehouseContacts_CreatedAtUtc");
            entity.Property(e => e.EmailAddress).HasMaxLength(254);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_WarehouseContacts_IsActive");
            entity.Property(e => e.PrimaryPhoneExtension)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.PrimaryPhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SecondaryPhoneExtension)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.SecondaryPhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Warehouse).WithMany(p => p.WarehouseContacts)
                .HasPrincipalKey(p => new { p.TenantId, p.WarehouseId })
                .HasForeignKey(d => new { d.TenantId, d.WarehouseId })
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WarehouseLocation>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.WarehouseId, e.LocationCode }, "UQ_WarehouseLocations_TenantId_WarehouseId_LocationCode").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.WarehouseLocationId }, "UQ_WarehouseLocations_TenantId_WarehouseLocationId").IsUnique();

            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_WarehouseLocations_CreatedAtUtc");
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_WarehouseLocations_IsActive");
            entity.Property(e => e.LocationCode).HasMaxLength(50);

            entity.HasOne(d => d.Warehouse).WithMany(p => p.WarehouseLocations)
                .HasPrincipalKey(p => new { p.TenantId, p.WarehouseId })
                .HasForeignKey(d => new { d.TenantId, d.WarehouseId })
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
