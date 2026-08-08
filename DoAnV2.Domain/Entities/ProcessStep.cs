using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>Một bước công việc trong ProductionProcess .</summary>
public class ProcessStep : BaseEntity
{
    public Guid ProcessId { get; set; }
    public ProductionProcess Process { get; set; } = null!;

    /// <summary>Map sang BatchStage để biết thuộc giai đoạn nào.</summary>
    public BatchStage Stage { get; set; }

    public string StepName { get; set; } = null!;
    public int OrderIndex { get; set; }
    public string? Description { get; set; }
}