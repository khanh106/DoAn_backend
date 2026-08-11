using DoAnV2.Application.Features.Admin.Blockchain.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Admin.Blockchain.Commands;

/// <summary>
/// TASK 11 - Mục 11.2: Admin phát lệnh Retry một giao dịch Blockchain bị FAILED.
/// Thực hiện theo BR-42:
///   1. Đọc lại record BlockchainTransaction bị lỗi.
///   2. Kiểm tra dữ liệu Off-chain + IPFS vẫn đầy đủ.
///   3. Gọi lại hàm SC tương ứng.
///   4. Cập nhật TransactionHash mới, BlockNumber, đổi Status = SUCCESS.
/// </summary>
public record RetryBlockchainTransactionCommand(
    Guid TransactionId) : IRequest<RetryTransactionResultDto>;
