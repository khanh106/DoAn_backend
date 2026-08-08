using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Infrastructure.Persistence;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;

    public UnitOfWork(
        ApplicationDbContext db,
        IUserRepository users,
        IBlockchainTransactionRepository blockchainTransactions,
        IFruitTypeRepository fruitTypes,
        IProductRepository products,
        IFarmAreaRepository farmAreas,
        IMaterialItemRepository materialItems,
        IInventoryLogRepository inventoryLogs,
        IBatchRepository batches,
        IBatchWorkerRepository batchWorkers)
    {
        _db = db;
        Users = users;
        BlockchainTransactions = blockchainTransactions;
        FruitTypes = fruitTypes;
        Products = products;
        FarmAreas = farmAreas;
        MaterialItems = materialItems;
        InventoryLogs = inventoryLogs;
        Batches = batches;
        BatchWorkers = batchWorkers;
    }

    public IUserRepository Users { get; }
    public IBlockchainTransactionRepository BlockchainTransactions { get; }
    public IFruitTypeRepository FruitTypes { get; }
    public IProductRepository Products { get; }
    public IFarmAreaRepository FarmAreas { get; }
    public IMaterialItemRepository MaterialItems { get; }
    public IInventoryLogRepository InventoryLogs { get; }
    public IBatchRepository Batches { get; }
    public IBatchWorkerRepository BatchWorkers { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
