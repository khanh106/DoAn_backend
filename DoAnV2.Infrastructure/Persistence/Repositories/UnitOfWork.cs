using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Infrastructure.Persistence;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;

    public UnitOfWork(
        IProcessorWorkerRepository processorWorkers,
        ApplicationDbContext db,
        IUserRepository users,
        IBlockchainTransactionRepository blockchainTransactions,
        IFruitTypeRepository fruitTypes,
        IProductRepository products,
        IFarmAreaRepository farmAreas,
        IMaterialItemRepository materialItems,
        IInventoryLogRepository inventoryLogs,
        IBatchRepository batches,
        IBatchWorkerRepository batchWorkers,
        ICultivationLogRepository cultivationLogs,
        IHarvestRepository harvests,
        IProcessingRepository processings,
        ISubBatchRepository subBatches,
        IInspectionRepository inspections,
        IPackagingRepository packagings,
        IShipmentRepository shipments,
        IQRCodeRepository qrCodes,
        IDistributorRepository distributors)
    {
        ProcessorWorkers = processorWorkers;
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
        CultivationLogs = cultivationLogs;
        Harvests = harvests;
        Processings = processings;
        SubBatches = subBatches;
        Inspections = inspections;
        Packagings = packagings;
        Shipments = shipments;
        QRCodes = qrCodes;
        Distributors = distributors;
    }

    public IProcessorWorkerRepository ProcessorWorkers { get; }
    public IUserRepository Users { get; }
    public IBlockchainTransactionRepository BlockchainTransactions { get; }
    public IFruitTypeRepository FruitTypes { get; }
    public IProductRepository Products { get; }
    public IFarmAreaRepository FarmAreas { get; }
    public IMaterialItemRepository MaterialItems { get; }
    public IInventoryLogRepository InventoryLogs { get; }
    public IBatchRepository Batches { get; }
    public IBatchWorkerRepository BatchWorkers { get; }
    public ICultivationLogRepository CultivationLogs { get; }
    public IHarvestRepository Harvests { get; }
    public IProcessingRepository Processings { get; }
    public ISubBatchRepository SubBatches { get; }
    public IInspectionRepository Inspections { get; }
    public IPackagingRepository Packagings { get; }
    public IShipmentRepository Shipments { get; }
    public IQRCodeRepository QRCodes { get; }
    public IDistributorRepository Distributors { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}