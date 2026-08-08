using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>Tài khoản hệ thống: Admin / Farmer / Processor / Retailer. Tham chiếu Chương 3, 7, 8.</summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;

    /// <summary>Email đăng nhập - Unique.</summary>
    public string Email { get; set; } = null!;

    /// <summary>BCrypt hash - tuyệt đối không lưu plaintext (BR-46.3).</summary>
    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    /// <summary>Địa chỉ ví Blockchain (0x...). Null nếu chưa cấp.</summary>
    public string? WalletAddress { get; set; }

    /// <summary>Private Key đã mã hóa AES cho Custodial Wallet (Chương 43).</summary>
    public string? EncryptedPrivateKey { get; set; }

    public UserStatus Status { get; set; } = UserStatus.PENDING;

    // Navigation
    public ICollection<BatchWorker> BatchWorkers { get; set; } = new List<BatchWorker>();
    public ICollection<Batch> RepresentedBatches { get; set; } = new List<Batch>();
    public ICollection<CultivationLog> CultivationLogs { get; set; } = new List<CultivationLog>();
    public ICollection<Harvest> Harvests { get; set; } = new List<Harvest>();
}