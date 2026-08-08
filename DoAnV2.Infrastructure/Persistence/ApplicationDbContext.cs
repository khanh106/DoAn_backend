using DoAnV2.Domain.Common;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using DoAnV2.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<FruitType> FruitTypes => Set<FruitType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<FarmArea> FarmAreas => Set<FarmArea>();
    public DbSet<MaterialItem> MaterialItems => Set<MaterialItem>();
    public DbSet<InventoryLog> InventoryLogs => Set<InventoryLog>();
    public DbSet<ProductionProcess> ProductionProcesses => Set<ProductionProcess>();
    public DbSet<ProcessStep> ProcessSteps => Set<ProcessStep>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchWorker> BatchWorkers => Set<BatchWorker>();
    public DbSet<CultivationLog> CultivationLogs => Set<CultivationLog>();
    public DbSet<Harvest> Harvests => Set<Harvest>();
    public DbSet<Processing> Processings => Set<Processing>();
    public DbSet<SubBatch> SubBatches => Set<SubBatch>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<Packaging> Packagings => Set<Packaging>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<QRCode> QRCodes => Set<QRCode>();
    public DbSet<BlockchainTransaction> BlockchainTransactions => Set<BlockchainTransaction>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ===== Soft Delete Global Filter =====
        foreach (var entityType in b.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { b });
            }
        }

        // ===== Enum -> String =====
        ConvertEnumsToString(b);

        // ===== Indexes & FKs =====
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Batch>(e =>
{
    e.HasIndex(x => x.BatchCode).IsUnique();
    e.HasOne(x => x.FruitType).WithMany().HasForeignKey(x => x.FruitTypeId).OnDelete(DeleteBehavior.Restrict);
    e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    e.HasOne(x => x.FarmArea).WithMany().HasForeignKey(x => x.FarmAreaId).OnDelete(DeleteBehavior.Restrict);
    e.HasOne(x => x.Processor).WithMany().HasForeignKey(x => x.ProcessorId).OnDelete(DeleteBehavior.Restrict);
    e.HasOne(x => x.RepresentativeWorker).WithMany(u => u.RepresentedBatches).HasForeignKey(x => x.RepresentativeWorkerId).OnDelete(DeleteBehavior.SetNull);
});

        b.Entity<SubBatch>(e =>
        {
            e.HasIndex(x => x.SubBatchCode).IsUnique();
            e.HasOne(x => x.ParentBatch).WithMany(p => p.SubBatches).HasForeignKey(x => x.ParentBatchId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<BatchWorker>(e =>
        {
            e.HasOne(x => x.Batch).WithMany(b => b.BatchWorkers).HasForeignKey(x => x.BatchId);
            e.HasOne(x => x.User).WithMany(u => u.BatchWorkers).HasForeignKey(x => x.UserId);
            e.HasIndex(x => new { x.BatchId, x.UserId }).IsUnique();
        });

        b.Entity<BlockchainTransaction>(e =>
        {
            e.HasIndex(x => x.TransactionHash).IsUnique();
            e.HasOne(x => x.Batch).WithMany(b => b.BlockchainTransactions).HasForeignKey(x => x.BatchId);
            e.HasOne(x => x.SubBatch).WithMany().HasForeignKey(x => x.SubBatchId);
        });

        b.Entity<InventoryLog>(e =>
        {
            // Tránh multiple cascade paths: User → InventoryLog và Batch → Processor → User
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Seed =====
        b.ApplySeed();
        var shadowFks = b.Model.GetEntityTypes()
    .SelectMany(e => e.GetDeclaredForeignKeys())
    .Where(fk => fk.Properties.Any(p => p.Name.EndsWith("Id1")))
    .ToList();
// Bước 1: Xóa FK trước
foreach (var fk in shadowFks)
{
    fk.DeclaringEntityType.RemoveForeignKey(fk);
}
// Bước 2: Xóa các shadow property sau khi FK đã được gỡ
var shadowPropNames = shadowFks
    .SelectMany(fk => fk.Properties.Select(p => p.Name))
    .Distinct()
    .ToList();
foreach (var entityType in b.Model.GetEntityTypes())
{
    foreach (var propName in shadowPropNames)
    {
        var shadowProp = entityType.FindProperty(propName);
        if (shadowProp != null)
        {
            entityType.RemoveProperty(shadowProp);
        }
    }
}
}

    private void SetSoftDeleteFilter<TEntity>(ModelBuilder b) where TEntity : BaseEntity
    {
        b.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConvertEnumsToString(ModelBuilder b)
    {
        foreach (var entityType in b.Model.GetEntityTypes())
        {
            foreach (var propertyInfo in entityType.ClrType.GetProperties())
            {
                var clrType = Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                              ?? propertyInfo.PropertyType;

                if (clrType.IsEnum)
                {
                    b.Entity(entityType.ClrType)
                        .Property(propertyInfo.Name)
                        .HasConversion<string>();
                }
            }
        }
    }


    public override int SaveChanges()
    {
        ApplyAudit();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ApplyAudit();
        return base.SaveChangesAsync(ct);
    }

    private void ApplyAudit()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTime.UtcNow;
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}