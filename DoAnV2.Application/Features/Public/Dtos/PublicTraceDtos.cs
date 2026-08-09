namespace DoAnV2.Application.Features.Public.Dtos;

/// <summary>
/// TASK 10 - Mục 10.1: Phản hồi tổng hợp cho API Truy xuất nguồn gốc công khai.
/// Kết hợp SubBatch → ParentBatch → FarmArea → Workers → CultivationLogs → Harvest →
/// Processing → Inspection → Packaging → Shipment → Retailer + lịch sử Blockchain.
/// Áp dụng cho endpoint:
///   - GET /api/v1/public/trace/{code}
/// </summary>
public record PublicTraceResponseDto(
    TargetInfoDto TargetInfo,
    ParentBatchDto? ParentBatch,
    FarmAreaDto? FarmArea,
    IReadOnlyList<WorkerDto> Workers,
    IReadOnlyList<CultivationLogDto> CultivationLogs,
    HarvestDto? Harvest,
    ProcessingDto? Processing,
    InspectionDto? Inspection,
    PackagingDto? Packaging,
    ShipmentDto? Shipment,
    IReadOnlyList<BlockchainHistoryDto> BlockchainHistory);

/// <summary>Thông tin đối tượng đang được truy xuất (SubBatch hoặc Batch).</summary>
public record TargetInfoDto(
    Guid Id,
    string Type,            // SUBBATCH | BATCH
    string Code,            // SUB-001 / BATCH-2026-001
    string ProductName,
    string FruitType,
    string CurrentStage,
    string? QrCodeUrl);

/// <summary>Thông tin Parent Batch (lô gốc) - BR-17.</summary>
public record ParentBatchDto(
    Guid BatchId,
    string BatchCode,
    DateTime PlantingDate);

/// <summary>Thông tin vùng trồng (Chương 11).</summary>
public record FarmAreaDto(
    string Name,
    string Province,
    string Gps,
    string? PlantingCode);

/// <summary>Thông tin nông dân/worker được gán vào lô.</summary>
public record WorkerDto(
    Guid UserId,
    string FullName,
    bool IsRepresentative);

/// <summary>Nhật ký canh tác (1 dòng) - Off-chain (BR-08).</summary>
public record CultivationLogDto(
    DateTime Date,
    string Activity,
    string Worker,
    IReadOnlyList<string> Images);

/// <summary>Thông tin thu hoạch (đại diện xác nhận).</summary>
public record HarvestDto(
    DateTime HarvestDate,
    double Quantity,
    string Unit,
    string RepresentativeWorker);

/// <summary>Thông tin sơ chế (1 bản ghi mới nhất).</summary>
public record ProcessingDto(
    string ProcessType,
    string Description,
    DateTime StartDate,
    DateTime? EndDate);

/// <summary>Thông tin kiểm định áp dụng cho SubBatch (hoặc fallback Parent).</summary>
public record InspectionDto(
    string DocumentName,
    string DocumentNumber,
    string Unit,
    DateTime InspectionDate,
    string Result,
    string CertificateFileUrl,
    string? Note);

/// <summary>Thông tin đóng gói áp dụng cho SubBatch (hoặc fallback Parent).</summary>
public record PackagingDto(
    DateTime PackDate,
    string Specification,
    string? Color,
    string? Smell,
    string? Standard,
    string? ImageUrl);

/// <summary>Thông tin vận chuyển tới Retailer.</summary>
public record ShipmentDto(
    string Carrier,
    string ShippingCode,
    DateTime ShippingDate,
    DateTime? ExpectedDate,
    DateTime? ReceivedDate,
    DateTime? ReadyForSaleDate,
    string RetailerName,
    string PickupLocation,
    string Destination);

/// <summary>Một mốc giao dịch On-chain trong lịch sử lô.</summary>
public record BlockchainHistoryDto(
    string Stage,
    string FunctionName,
    string TxHash,
    long? BlockNumber,
    DateTime Timestamp,
    string ActorWallet,
    string Status);
