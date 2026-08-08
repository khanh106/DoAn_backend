using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Infrastructure.Persistence;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;

    public UnitOfWork(
        ApplicationDbContext db,
        IUserRepository users,
        IBlockchainTransactionRepository blockchainTransactions)
    {
        _db = db;
        Users = users;
        BlockchainTransactions = blockchainTransactions;
    }

    public IUserRepository Users { get; }
    public IBlockchainTransactionRepository BlockchainTransactions { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
