using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using DoAnV2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;

    public UserRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<User>> GetPendingUsersAsync(CancellationToken ct = default)
        => await _db.Users
            .Include(u => u.Role)
            .Where(u => u.Status == UserStatus.PENDING)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => await _db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _db.Users.AddAsync(user, ct);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idSet = ids.Distinct().ToHashSet();
        if (idSet.Count == 0)
            return Array.Empty<User>();

        return await _db.Users
            .Include(u => u.Role)
            .Where(u => idSet.Contains(u.Id))
            .ToListAsync(ct);
    }
}
