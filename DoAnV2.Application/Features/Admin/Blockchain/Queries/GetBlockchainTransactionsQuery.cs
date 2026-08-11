using DoAnV2.Application.Features.Admin.Blockchain.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.Admin.Blockchain.Queries;

/// <summary>
/// TASK 11 - Mục 11.2: Lấy danh sách giao dịch Blockchain với filter
/// (Status / FunctionName / BatchId).
/// </summary>
public record GetBlockchainTransactionsQuery(
    TransactionStatus? Status,
    string? FunctionName,
    Guid? BatchId) : IRequest<IReadOnlyList<BlockchainTransactionDto>>;
