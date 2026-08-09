using DoAnV2.Application.Features.Public.Dtos;
using DoAnV2.Application.Features.Public.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 10 - Mục 10.1 &amp; 10.2: API Truy xuất nguồn gốc công khai (Public/Guest).
/// Cho phép Người tiêu dùng quét QR / gửi mã lô và xem toàn bộ chuỗi cung ứng
/// mà KHÔNG cần đăng nhập (AllowAnonymous).
///
/// Thuật toán Truy ngược (BR-16, BR-17): SubBatch ➔ Parent Batch ➔ FarmArea ➔ Workers ➔
/// CultivationLogs ➔ Harvest ➔ Processing ➔ Inspection ➔ Packaging ➔ Shipment ➔ Retailer.
///
///   - GET /api/v1/public/trace/{code}
///   - GET /api/v1/public/trace/{code}/blockchain
///   - GET /api/v1/public/trace/{code}/qr/verify
///
/// "code" có thể là BatchId/SubBatchId (Guid), BatchCode, SubBatchCode, hoặc QRValue.
///
/// Lưu ý (TASK 10 - Mục 10.1 bảo vệ):
///   - Rate-limit cao cho endpoint public (60 req/IP/phút) - Sẽ được bật ở TASK 11/L3
///     bằng cách wrap policy "public-trace" qua AspNetCoreRateLimit.
///   - Hiện tại chưa bật để không phá vỡ các test; có thể bật bằng cách thêm
///     [EnableRateLimiting("public-trace")] + đăng ký policy trong Program.cs.
///   - Không trả thông tin nhạy cảm: Private Key, password hash, email Processor.
///     (Composite DTO chỉ chứa các field công khai).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/public/trace")]
public class PublicTraceabilityController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicTraceabilityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// GET /api/v1/public/trace/{code}
    /// Trả về composite DTO gồm toàn bộ chuỗi cung ứng (targetInfo, parentBatch, farmArea,
    /// workers, cultivationLogs, harvest, processing, inspection, packaging, shipment,
    /// blockchainHistory). Xem PublicTraceResponseDto.
    /// </summary>
    [HttpGet("{code}")]
    public async Task<ActionResult<PublicTraceResponseDto>> GetByCode(
        [FromRoute] string code,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicTraceByCodeQuery(code), ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/public/trace/{code}/blockchain
    /// Trả về CHỈ phần lịch sử giao dịch On-chain (BlockchainHistory) - tách riêng để client
    /// hiển thị riêng tab "Blockchain" nếu muốn.
    /// </summary>
    [HttpGet("{code}/blockchain")]
    public async Task<ActionResult<IReadOnlyList<BlockchainHistoryDto>>> GetBlockchainOnly(
        [FromRoute] string code,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicTraceByCodeQuery(code), ct);
        return Ok(result.BlockchainHistory);
    }

    /// <summary>
    /// GET /api/v1/public/trace/{code}/qr/verify
    /// Trả 200 OK + QR info nếu QR hợp lệ; trả 404 nếu không tìm thấy / không active.
    /// </summary>
    [HttpGet("{code}/qr/verify")]
    public async Task<ActionResult<object>> VerifyQr(
        [FromRoute] string code,
        [FromServices] DoAnV2.Application.Common.Interfaces.IUnitOfWork uow,
        CancellationToken ct)
    {
        // 1. Thử tra cứu như là QRValue
        var qr = await uow.QRCodes.GetByQRValueAsync(code, ct);
        if (qr != null)
        {
            return Ok(new
            {
                valid = qr.Status == DoAnV2.Domain.Entities.QRCodeStatus.ACTIVE,
                targetType = qr.TargetType.ToString(),
                targetId = qr.TargetId,
                qrCodeId = qr.Id,
                createdAt = qr.CreatedAt
            });
        }

        // 2. Nếu không phải QRValue thì coi như Batch/SubBatch code ⇒ hợp lệ nếu lookup được
        try
        {
            var trace = await _mediator.Send(new GetPublicTraceByCodeQuery(code), ct);
            return Ok(new
            {
                valid = true,
                targetType = trace.TargetInfo.Type,
                targetId = trace.TargetInfo.Id,
                productName = trace.TargetInfo.ProductName
            });
        }
        catch (DoAnV2.Application.Common.Exceptions.NotFoundException)
        {
            return NotFound(new { valid = false, message = $"Không tìm thấy lô với mã '{code}'." });
        }
    }
}
