namespace DoAnV2.Application.Features.Shipments.Dtos;

/// <summary>
/// DTO trả về sau khi Processor tạo vận đơn (TASK 09 - Mục 9.1).
/// Áp dụng cho cả Parent Batch (shipParent) và SubBatch (shipSub).
/// BR-18: Chỉ vận chuyển khi lô ở PACKAGED.
/// </summary>
public record ShipmentResponseDto(
    Guid ShipmentId,
    string AssetType,
    Guid? BatchId,
    string? BatchCode,
    Guid? SubBatchId,
    string? SubBatchCode,
    Guid RetailerId,
    string RetailerName,
    string PickupLocation,
    string Destination,
    string CarrierInfo,
    string ShippingCode,
    DateTime ShippingDate,
    DateTime? ExpectedDate,
    double Weight,
    string? MetadataURI,
    string? DataHash,
    string? ShipTransactionHash,
    string CurrentStage,
    DateTime CreatedAt);

/// <summary>
/// DTO trả về sau khi Retailer xác nhận tiếp nhận (receive) hoặc đưa lên kệ (ready-for-sale).
/// </summary>
public record RetailerActionResponseDto(
    Guid ShipmentId,
    string AssetType,
    Guid? BatchId,
    string? BatchCode,
    Guid? SubBatchId,
    string? SubBatchCode,
    string CurrentStage,
    string? ReceiveMetadataURI,
    string? ReceiveDataHash,
    string? ReceiveTransactionHash,
    DateTime? ReceivedDate,
    string? ReadyMetadataURI,
    string? ReadyDataHash,
    string? ReadyTransactionHash,
    DateTime? ReadyForSaleDate,
    string? TransactionHash,
    DateTime? UpdatedAt);

/// <summary>
/// Lịch sử các vận đơn của 1 lô (Parent hoặc Sub).
/// </summary>
public record ShipmentHistoryDto(
    Guid Id,
    string AssetType,
    Guid? BatchId,
    string? BatchCode,
    Guid? SubBatchId,
    string? SubBatchCode,
    Guid RetailerId,
    string RetailerName,
    string PickupLocation,
    string Destination,
    string CarrierInfo,
    string ShippingCode,
    DateTime ShippingDate,
    DateTime? ExpectedDate,
    DateTime? ReceivedDate,
    DateTime? ReadyForSaleDate,
    double Weight,
    string? ShipTransactionHash,
    string? ReceiveTransactionHash,
    string? ReadyTransactionHash,
    DateTime CreatedAt);