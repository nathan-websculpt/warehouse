using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Data.Entities;

using WarehouseEntity = Warehouse.Api.Data.Entities.Warehouse;

namespace Warehouse.Api.Data;

public partial class WarehouseDbContext : DbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<WarehouseEntity> Warehouses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
