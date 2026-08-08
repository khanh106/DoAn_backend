using DoAnV2.Domain.Common;

namespace DoAnV2.Domain.Entities;

/// <summary>Quy trình sản xuất - template do Processor tạo.</summary>
public class ProductionProcess : BaseEntity
{
    public Guid ProcessorId { get; set; }
    public User Processor { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<ProcessStep> Steps { get; set; } = new List<ProcessStep>();
}