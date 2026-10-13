using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data.Entities;

using WarehouseEntity = Warehouse.Api.Data.Entities.Warehouse;

namespace Warehouse.Api.Data;

public partial class WarehouseDbContext : DbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<InventoryMovement> InventoryMovements { get; set; }

    public virtual DbSet<InventoryTransfer> InventoryTransfers { get; set; }

    public virtual DbSet<InventoryTransferLine> InventoryTransferLines { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<WarehouseEntity> Warehouses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_InventoryMovements_CreatedAtUtc");
            entity.Property(e => e.Quantity).HasColumnType("decimal(19, 4)");

            entity.HasOne(d => d.InventoryTransferLine).WithMany(p => p.InventoryMovements)
                .HasPrincipalKey(p => new { p.TenantId, p.InventoryTransferLineId, p.InventoryLotId })
                .HasForeignKey(d => new { d.TenantId, d.InventoryTransferLineId, d.InventoryLotId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryMovements_InventoryTransferLines_Tenant_Line_Lot");
        });

        modelBuilder.Entity<InventoryTransfer>(entity =>
        {
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
        });

        modelBuilder.Entity<InventoryTransferLine>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.InventoryTransferId, e.InventoryLotId }, "UQ_InventoryTransferLines_TenantId_InventoryTransferId_InventoryLotId").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.InventoryTransferLineId, e.InventoryLotId }, "UQ_InventoryTransferLines_TenantId_InventoryTransferLineId_InventoryLotId").IsUnique();

            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_InventoryTransferLines_CreatedAtUtc");
            entity.Property(e => e.QuantityRequested).HasColumnType("decimal(19, 4)");

            entity.HasOne(d => d.Tenant).WithMany(p => p.InventoryTransferLines)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.InventoryTransfer).WithMany(p => p.InventoryTransferLines)
                .HasPrincipalKey(p => new { p.TenantId, p.InventoryTransferId })
                .HasForeignKey(d => new { d.TenantId, d.InventoryTransferId })
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.TenantPublicId, "UQ_Tenants_TenantPublicId").IsUnique();

            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_Tenants_CreatedAtUtc");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Tenants_IsActive");
            entity.Property(e => e.TenantName).HasMaxLength(200);
            entity.Property(e => e.TenantPublicId).HasDefaultValueSql("(newid())", "DF_Tenants_TenantPublicId");
        });

        modelBuilder.Entity<WarehouseEntity>(entity =>
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
