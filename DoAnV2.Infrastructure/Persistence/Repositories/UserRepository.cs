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
    public async Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken ct = default)
        => await _db.Users
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
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

    public async Task<IReadOnlyList<User>> SearchFarmersAsync(string? keyword, CancellationToken ct = default)
    {
        var query = _db.Users
            .Include(u => u.Role)
            .Where(u => u.Role!.RoleName == RoleType.FARMER && u.Status == UserStatus.APPROVED);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(k) || u.Email.ToLower().Contains(k) || u.Phone.Contains(k));
        }

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<User>> SearchRetailersAsync(string? keyword, CancellationToken ct = default)
    {
        var query = _db.Users
            .Include(u => u.Role)
            .Where(u => u.Role!.RoleName == RoleType.RETAILER && u.Status == UserStatus.APPROVED);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(k) || u.Email.ToLower().Contains(k) || u.Phone.Contains(k));
        }

        return await query.ToListAsync(ct);
    }

    public async Task<UserStats> GetStatsAsync(CancellationToken ct = default)
    {
        // Lấy Role + Status 1 lần rồi aggregate in-memory.
        var users = await _db.Users
            .Include(u => u.Role)
            .Select(u => new { u.Role!.RoleName, u.Status })
            .ToListAsync(ct);

        return new UserStats(
            Total: users.Count,
            Farmers: users.Count(u => u.RoleName == RoleType.FARMER),
            Processors: users.Count(u => u.RoleName == RoleType.PROCESSOR),
            Retailers: users.Count(u => u.RoleName == RoleType.RETAILER),
            Active: users.Count(u => u.Status == UserStatus.APPROVED),
            Pending: users.Count(u => u.Status == UserStatus.PENDING),
            Locked: users.Count(u => u.Status == UserStatus.LOCKED));
    }
}
