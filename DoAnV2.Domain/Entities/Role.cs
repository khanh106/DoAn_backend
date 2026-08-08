using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>Vai trò hệ thống (Chương 7.3): ADMIN, FARMER, PROCESSOR, RETAILER - cố định 4 role.</summary>
public class Role : BaseEntity
{
    /// <summary>Khóa chính int - ép cứng 1..4.</summary>
    public int Id { get; set; }

    public RoleType RoleName { get; set; }
    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}