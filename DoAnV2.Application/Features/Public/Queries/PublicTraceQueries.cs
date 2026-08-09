using DoAnV2.Application.Features.Public.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Public.Queries;

/// <summary>
/// TASK 10 - Mục 10.1 &amp; 10.2: Lấy dữ liệu truy xuất nguồn gốc công khai theo code.
/// "code" có thể là:
///   - Guid của Batch/SubBatch (lookup trực tiếp).
///   - BatchCode (VD: "BATCH-2026-001").
///   - SubBatchCode (VD: "SUB-2026-001-1").
///   - QRCode.QRValue (URL truy xuất).
/// </summary>
public record GetPublicTraceByCodeQuery(string Code)
    : IRequest<PublicTraceResponseDto>;
