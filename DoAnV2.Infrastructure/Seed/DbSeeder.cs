using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static void ApplySeed(this ModelBuilder b)
    {
        // ===== Roles =====
        b.Entity<Role>().HasData(
            new Role { Id = 1, RoleName = RoleType.ADMIN, Description = "Quản trị hệ thống" },
            new Role { Id = 2, RoleName = RoleType.FARMER, Description = "Nông dân / Công nhân" },
            new Role { Id = 3, RoleName = RoleType.PROCESSOR, Description = "Hợp tác xã / Doanh nghiệp" },
            new Role { Id = 4, RoleName = RoleType.RETAILER, Description = "Cửa hàng bán lẻ" }
        );

        // ===== Admin User =====
        var adminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        b.Entity<User>().HasData(new User
        {
            Id = adminId,
            FullName = "System Administrator",
            Phone = "0000000000",
            Email = "admin@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
            RoleId = 1,
            Status = UserStatus.APPROVED,
            CreatedAt = DateTime.UtcNow
        });
    }
}