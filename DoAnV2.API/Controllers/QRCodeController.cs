using DoAnV2.Application.Features.QRCodes.Commands;
using DoAnV2.Application.Features.QRCodes.Dtos;
using DoAnV2.Application.Features.QRCodes.Queries;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 08 - Mục 8.3: API Sinh mã QR Code truy xuất nguồn gốc (Processor).
///   - POST /api/v1/processor/qrcodes/generate
///   - GET  /api/v1/processor/qrcodes/{targetType}/{targetId}
///
/// BR-14: QR thương mại (BOX/COMMERCIAL) chỉ phát hành khi đã PACKAGED.
/// BR-17: Quét QR � truy ngược SubBatch → Batch.
/// </summary>
[ApiController]
[Authorize(Policy = "RequireProcessor")]
public class QRCodeController : ControllerBase
{
    private readonly IMediator _mediator;

    public QRCodeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/v1/processor/qrcodes/generate
    /// Body: { "targetType": "BATCH" | "SUBBATCH" | "BOX" | "COMMERCIAL", "targetId": "guid" }
    /// Trả về: ảnh QR (Base64 PNG) + URL truy xuất.
    /// </summary>
    [HttpPost("api/v1/processor/qrcodes/generate")]
    public async Task<ActionResult<GenerateQRCodeResponseDto>> Generate(
        [FromBody] GenerateQRCodeRequest body,
        CancellationToken ct)
    {
        if (!Enum.TryParse<QRTargetType>(body.TargetType, ignoreCase: true, out var targetType))
            throw new DoAnV2.Application.Common.Exceptions.ValidationException(
                "TargetType chỉ chấp nhận BATCH / SUBBATCH / BOX / COMMERCIAL.");

        var cmd = new GenerateQRCodeCommand(targetType, body.TargetId);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/processor/qrcodes/{targetType}/{targetId}
    /// Lấy danh sách QR code đã phát hành cho 1 đối tượng.
    /// </summary>
    [HttpGet("api/v1/processor/qrcodes/{targetType}/{targetId:guid}")]
    public async Task<ActionResult<IReadOnlyList<QRCodeInfoDto>>> GetByTarget(
        [FromRoute] string targetType,
        [FromRoute] Guid targetId,
        CancellationToken ct)
    {
        if (!Enum.TryParse<QRTargetType>(targetType, ignoreCase: true, out var parsed))
            throw new DoAnV2.Application.Common.Exceptions.ValidationException(
                "TargetType chỉ chấp nhận BATCH / SUBBATCH / BOX / COMMERCIAL.");

        var result = await _mediator.Send(new GetQRCodesByTargetQuery(parsed, targetId), ct);
        return Ok(result);
    }
}

/// <summary>Body cho POST /api/v1/processor/qrcodes/generate.</summary>
public record GenerateQRCodeRequest(
    string TargetType,
    Guid TargetId);
