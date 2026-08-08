using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IBlockchainTransactionRepository BlockchainTransactions { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetPendingUsersAsync(CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}