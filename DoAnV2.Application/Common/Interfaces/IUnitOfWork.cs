using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IBlockchainTransactionRepository BlockchainTransactions { get; }
    IFruitTypeRepository FruitTypes { get; }
    IProductRepository Products { get; }
    IFarmAreaRepository FarmAreas { get; }
    IMaterialItemRepository MaterialItems { get; }
    IInventoryLogRepository InventoryLogs { get; }
    IBatchRepository Batches { get; }
    IBatchWorkerRepository BatchWorkers { get; }
    ICultivationLogRepository CultivationLogs { get; }
    IHarvestRepository Harvests { get; }
    IProcessingRepository Processings { get; }
    ISubBatchRepository SubBatches { get; }
    IInspectionRepository Inspections { get; }
    IPackagingRepository Packagings { get; }
    IShipmentRepository Shipments { get; }
    IQRCodeRepository QRCodes { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetPendingUsersAsync(CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>Lookup nhiều user theo danh sách Guid - dùng để validate assignedWorkerIds.</summary>
    Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}