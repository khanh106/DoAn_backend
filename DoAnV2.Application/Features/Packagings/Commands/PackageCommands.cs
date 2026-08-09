using DoAnV2.Application.Features.Packagings.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace DoAnV2.Application.Features.Packagings.Commands;

/// <summary>
/// Input cho Packaging Controller - JSON body chứa thông tin đóng gói + list file ảnh.
/// TASK 08 - Mục 8.2.
/// </summary>
public record PackageInputDto(
    DateTime PackDate,
    double Weight,
    string Specification,
    string? UsageGuide,
    string? StorageGuide,
    string? Color,
    string? Smell,
    string? Standard,
    string? Note);

/// <summary>
/// TASK 08 - Mục 8.2: Processor đóng gói thương mại cho Parent Batch (gọi SC packageParent).
/// BR-14: Chỉ đóng gói khi kiểm định đã ĐẠT (INSPECTION_PASSED).
/// </summary>
public record PackageParentCommand(
    Guid BatchId,
    PackageInputDto Input,
    IReadOnlyList<IFormFile> Images) : IRequest<PackagingResponseDto>;

/// <summary>
/// TASK 08 - Mục 8.2: Processor đóng gói thương mại cho SubBatch (gọi SC packageSub).
/// BR-14 + BR-16: Chỉ đóng gói khi SubBatch INSPECTION_PASSED.
/// </summary>
public record PackageSubCommand(
    Guid SubBatchId,
    PackageInputDto Input,
    IReadOnlyList<IFormFile> Images) : IRequest<PackagingResponseDto>;
