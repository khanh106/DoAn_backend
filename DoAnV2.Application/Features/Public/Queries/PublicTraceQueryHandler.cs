using System.Text.Json;
using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Public.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Public.Queries;

/// <summary>
/// TASK 10 - Mục 10.1 &amp; 10.2: Handler tổng hợp dữ liệu truy xuất nguồn gốc công khai.
/// Trả về PublicTraceResponseDto (composite JSON) cho phép Người tiêu dùng (Guest) quét QR
/// và xem toàn bộ chuỗi cung ứng mà KHÔNG cần đăng nhập.
///
/// Thuật toán Truy ngược (BR-16, BR-17):
///   1. Resolve "code" về (TargetType, TargetId):
///      - Guid → thử SubBatch trước, fallback Batch.
///      - String non-Guid → thử QRCode.QRValue, BatchCode, SubBatchCode.
///   2. Nếu target là SubBatch → lấy ParentBatch (kèm toàn bộ chain).
///      Nếu target là Parent Batch → lấy SubBatch đầu tiên (nếu có) làm "current".
///   3. Build timeline từ FarmArea ➔ Workers ➔ CultivationLogs ➔ Harvest ➔ Processing
///      ➔ Inspection (SubBatch) ➔ Packaging (SubBatch) ➔ Shipment (SubBatch) ➔ Retailer.
///   4. Lấy BlockchainHistory từ bảng BlockchainTransaction (On-chain records).
/// </summary>
public class PublicTraceQueryHandler
    : IRequestHandler<GetPublicTraceByCodeQuery, PublicTraceResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly TraceOptions _traceOptions;
    private readonly ILogger<PublicTraceQueryHandler> _logger;

    public PublicTraceQueryHandler(
        IUnitOfWork uow,
        Microsoft.Extensions.Options.IOptions<TraceOptions> traceOptions,
        ILogger<PublicTraceQueryHandler> logger)
    {
        _uow = uow;
        _traceOptions = traceOptions.Value;
        _logger = logger;
    }

    // ================== Entry point ==================

    public async Task<PublicTraceResponseDto> Handle(
        GetPublicTraceByCodeQuery req, CancellationToken ct)
    {
        var resolved = await ResolveTargetAsync(req.Code, ct);
        var dto = await BuildTraceAsync(
            resolved.subBatch,
            resolved.parentBatch,
            resolved.targetType,
            resolved.targetCode,
            ct);
        return dto;
    }

    // ================== Resolve code → entity ==================

    private async Task<(SubBatch? subBatch, Batch? parentBatch, string targetType, string targetCode)>
        ResolveTargetAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ValidationException("Mã truy xuất (code) không được rỗng.");

        var trimmed = code.Trim();

        // Case 1: code là Guid → thử SubBatch trước, fallback Batch.
        if (Guid.TryParse(trimmed, out var guid))
        {
            var sub = await _uow.SubBatches.GetByIdWithDetailsAsync(guid, ct);
            if (sub != null)
                return (sub, sub.ParentBatch, "SUBBATCH", sub.SubBatchCode);

            var batch = await _uow.Batches.GetByIdWithFullChainAsync(guid, ct);
            if (batch != null)
            {
                // Nếu batch có SubBatch, dùng SubBatch đầu tiên làm "current target"
                // để hiển thị chuỗi SubBatch Inspection → Packaging → Shipment.
                var firstSub = batch.SubBatches.FirstOrDefault();
                if (firstSub != null)
                {
                    var subWithDetails = await _uow.SubBatches.GetByIdWithDetailsAsync(firstSub.Id, ct);
                    return (subWithDetails, batch, "SUBBATCH", firstSub.SubBatchCode);
                }
                return (null, batch, "BATCH", batch.BatchCode);
            }

            throw new NotFoundException($"Không tìm thấy lô với Id '{trimmed}'.");
        }

        // Case 2: code là QRValue (URL truy xuất)
        var qrCode = await _uow.QRCodes.GetByQRValueAsync(trimmed, ct);
        if (qrCode != null)
        {
            return qrCode.TargetType switch
            {
                QRTargetType.BATCH => await ResolveByBatchGuid(qrCode.TargetId, ct),
                QRTargetType.SUBBATCH => await ResolveBySubBatchGuid(qrCode.TargetId, ct),
                _ => throw new ValidationException(
                    $"QRCode loại '{qrCode.TargetType}' chưa hỗ trợ truy xuất công khai.")
            };
        }

        // Case 3: code là BatchCode (string)
        var batchByCode = await _uow.Batches.GetByBatchCodeAsync(trimmed, ct);
        if (batchByCode != null)
            return await ResolveByBatchGuid(batchByCode.Id, ct);

        // Case 4: code là SubBatchCode
        var subByCode = await _uow.SubBatches.GetBySubBatchCodeAsync(trimmed, ct);
        if (subByCode != null)
            return await ResolveBySubBatchGuid(subByCode.Id, ct);

        throw new NotFoundException($"Không tìm thấy lô với mã '{trimmed}'.");
    }

    private async Task<(SubBatch?, Batch, string, string)> ResolveByBatchGuid(Guid id, CancellationToken ct)
    {
        var batch = await _uow.Batches.GetByIdWithFullChainAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {id}.");

        var firstSub = batch.SubBatches.FirstOrDefault();
        if (firstSub != null)
        {
            var subWithDetails = await _uow.SubBatches.GetByIdWithDetailsAsync(firstSub.Id, ct);
            return (subWithDetails, batch, "SUBBATCH", firstSub.SubBatchCode);
        }
        return (null, batch, "BATCH", batch.BatchCode);
    }

    private async Task<(SubBatch, Batch, string, string)> ResolveBySubBatchGuid(Guid id, CancellationToken ct)
    {
        var sub = await _uow.SubBatches.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy SubBatch {id}.");
        return (sub, sub.ParentBatch, "SUBBATCH", sub.SubBatchCode);
    }

    // ================== Build composite DTO ==================

    private async Task<PublicTraceResponseDto> BuildTraceAsync(
        SubBatch? subBatch,
        Batch? parentBatch,
        string targetType,
        string targetCode,
        CancellationToken ct)
    {
        if (parentBatch == null)
            throw new NotFoundException("Không tìm thấy Parent Batch cho mã truy xuất này.");

        // ============ targetInfo ============
        var currentStage = subBatch?.CurrentStage ?? parentBatch.CurrentStage;
        var productName = parentBatch.Product?.Name ?? string.Empty;
        var fruitTypeName = parentBatch.FruitType?.Name ?? string.Empty;
        var qrCodeUrl = $"{_traceOptions.TraceBaseUrl}?code={(subBatch?.Id ?? parentBatch.Id)}";

        var targetInfo = new TargetInfoDto(
            Id: subBatch?.Id ?? parentBatch.Id,
            Type: targetType,
            Code: targetCode,
            ProductName: productName,
            FruitType: fruitTypeName,
            CurrentStage: currentStage.ToString(),
            QrCodeUrl: qrCodeUrl);

        // ============ parentBatch ============
        ParentBatchDto? parentDto = null;
        if (subBatch != null)
        {
            parentDto = new ParentBatchDto(
                BatchId: parentBatch.Id,
                BatchCode: parentBatch.BatchCode,
                PlantingDate: parentBatch.PlantingDate);
        }

        // ============ farmArea ============
        FarmAreaDto? farmAreaDto = null;
        if (parentBatch.FarmArea != null)
        {
            farmAreaDto = new FarmAreaDto(
                Name: parentBatch.FarmArea.Name,
                Province: parentBatch.FarmArea.Province,
                Gps: parentBatch.FarmArea.GPS ?? string.Empty,
                PlantingCode: parentBatch.FarmArea.PlantingCode);
        }

        // ============ workers ============
        var workers = parentBatch.BatchWorkers
            .OrderByDescending(w => w.IsRepresentative)
            .ThenBy(w => w.AssignedDate)
            .Select(w => new WorkerDto(
                UserId: w.UserId,
                FullName: w.User?.FullName ?? string.Empty,
                IsRepresentative: w.IsRepresentative))
            .ToList();

        // ============ cultivationLogs ============
        var cultivationLogs = parentBatch.CultivationLogs
            .OrderBy(c => c.LogDate)
            .Select(c => new CultivationLogDto(
                Date: c.LogDate,
                Activity: c.ActivityType,
                Worker: c.User?.FullName ?? string.Empty,
                Images: ParseImageUrls(c.ImageUrlsJson)))
            .ToList();

        // ============ harvest (lấy bản ghi mới nhất) ============
        HarvestDto? harvestDto = null;
        var latestHarvest = parentBatch.Harvests
            .OrderByDescending(h => h.HarvestDate)
            .FirstOrDefault();
        if (latestHarvest != null)
        {
            harvestDto = new HarvestDto(
                HarvestDate: latestHarvest.HarvestDate,
                Quantity: latestHarvest.Quantity,
                Unit: latestHarvest.Unit,
                RepresentativeWorker: latestHarvest.RepresentativeUser?.FullName
                    ?? parentBatch.RepresentativeWorker?.FullName
                    ?? string.Empty);
        }

        // ============ processing (lấy bản ghi mới nhất) ============
        ProcessingDto? processingDto = null;
        var latestProcessing = parentBatch.Processings
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefault();
        if (latestProcessing != null)
        {
            processingDto = new ProcessingDto(
                ProcessType: latestProcessing.ProcessType,
                Description: latestProcessing.Description,
                StartDate: latestProcessing.StartDate,
                EndDate: latestProcessing.EndDate);
        }

        // ============ inspection (ưu tiên SubBatch, fallback Parent) ============
        InspectionDto? inspectionDto = null;
        var inspections = subBatch?.Inspections ?? parentBatch.Inspections;
        var latestInspection = inspections
            .OrderByDescending(i => i.InspectionDate)
            .FirstOrDefault();
        if (latestInspection != null)
        {
            inspectionDto = new InspectionDto(
                DocumentName: latestInspection.DocumentName,
                DocumentNumber: latestInspection.DocumentNumber,
                Unit: latestInspection.InspectionUnit,
                InspectionDate: latestInspection.InspectionDate,
                Result: latestInspection.Result.ToString(),
                CertificateFileUrl: latestInspection.FileURI ?? string.Empty,
                Note: latestInspection.Note);
        }

        // ============ packaging (ưu tiên SubBatch, fallback Parent) ============
        PackagingDto? packagingDto = null;
        var packagings = subBatch?.Packagings ?? parentBatch.Packagings;
        var latestPackaging = packagings
            .OrderByDescending(p => p.PackDate)
            .FirstOrDefault();
        if (latestPackaging != null)
        {
            packagingDto = new PackagingDto(
                PackDate: latestPackaging.PackDate,
                Specification: latestPackaging.Specification,
                Color: latestPackaging.Color,
                Smell: latestPackaging.Smell,
                Standard: latestPackaging.Standard,
                ImageUrl: ParseImageUrls(latestPackaging.ImageUrlsJson).FirstOrDefault());
        }

        // ============ shipment (ưu tiên SubBatch, fallback Parent) ============
        ShipmentDto? shipmentDto = null;
        var shipments = subBatch?.Shipments ?? parentBatch.Shipments;
        var latestShipment = shipments
            .OrderByDescending(s => s.ShippingDate)
            .FirstOrDefault();
        if (latestShipment != null)
        {
            shipmentDto = new ShipmentDto(
                Carrier: latestShipment.CarrierInfo,
                ShippingCode: latestShipment.ShippingCode,
                ShippingDate: latestShipment.ShippingDate,
                ExpectedDate: latestShipment.ExpectedDate,
                ReceivedDate: latestShipment.ReceivedDate,
                ReadyForSaleDate: latestShipment.ReadyForSaleDate,
                RetailerName: latestShipment.Retailer?.FullName ?? string.Empty,
                PickupLocation: latestShipment.PickupLocation,
                Destination: latestShipment.Destination);
        }

        // ============ blockchainHistory ============
        var blockchainHistory = await LoadBlockchainHistoryAsync(parentBatch.Id, subBatch?.Id, ct);

        _logger.LogInformation(
            "PublicTrace: code='{Code}', targetType={TargetType}, parentBatchId={ParentId}, subBatchId={SubId}, blockchainTxCount={TxCount}",
            targetCode, targetType, parentBatch.Id, subBatch?.Id, blockchainHistory.Count);

        return new PublicTraceResponseDto(
            TargetInfo: targetInfo,
            ParentBatch: parentDto,
            FarmArea: farmAreaDto,
            Workers: workers,
            CultivationLogs: cultivationLogs,
            Harvest: harvestDto,
            Processing: processingDto,
            Inspection: inspectionDto,
            Packaging: packagingDto,
            Shipment: shipmentDto,
            BlockchainHistory: blockchainHistory);
    }

    private async Task<IReadOnlyList<BlockchainHistoryDto>> LoadBlockchainHistoryAsync(
        Guid parentBatchId, Guid? subBatchId, CancellationToken ct)
    {
        IReadOnlyList<BlockchainTransaction> txs;
        if (subBatchId.HasValue)
        {
            txs = await _uow.BlockchainTransactions.GetHistoryForSubBatchAsync(subBatchId.Value, ct);
        }
        else
        {
            txs = await _uow.BlockchainTransactions.GetHistoryForBatchAsync(parentBatchId, ct);
        }

        return txs.Select(t => new BlockchainHistoryDto(
            Stage: MapFunctionToStage(t.FunctionName),
            FunctionName: t.FunctionName,
            TxHash: t.TransactionHash,
            BlockNumber: t.BlockNumber,
            Timestamp: t.Timestamp,
            ActorWallet: t.WalletAddress,
            Status: t.Status.ToString())).ToList();
    }

    /// <summary>
    /// Map tên hàm Smart Contract -> stage enum (string) tương ứng trong BatchStage.
    /// </summary>
    private static string MapFunctionToStage(string functionName)
    {
        if (string.IsNullOrWhiteSpace(functionName)) return string.Empty;
        return functionName.ToLowerInvariant() switch
        {
            "createbatch" => BatchStage.STAGE_PLANTING.ToString(),
            "harvestbatch" => BatchStage.STAGE_HARVESTED.ToString(),
            "receivebatch" => BatchStage.STAGE_RECEIVED.ToString(),
            "processbatch" => BatchStage.STAGE_PROCESSED.ToString(),
            "classifyonlybatch" or "splitbatch" => BatchStage.STAGE_SORTED.ToString(),
            "inspectparent" or "inspectsub" => "INSPECTION_PASSED",
            "packageparent" or "packagesub" => BatchStage.PACKAGED.ToString(),
            "shipparent" or "shipsub" => BatchStage.STAGE_SHIPPING.ToString(),
            "receiveparent" or "receivesub" => BatchStage.RECEIVED_AT_RETAILER.ToString(),
            "readyparent" or "readysub" => BatchStage.READY_FOR_SALE.ToString(),
            _ => functionName.ToUpperInvariant()
        };
    }

    private static IReadOnlyList<string> ParseImageUrls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
