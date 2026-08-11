using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Admin.Blockchain.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Admin.Blockchain.Queries;

/// <summary>
/// TASK 11 - Mục 11.2: Handler trả về danh sách giao dịch Blockchain cho Admin.
/// </summary>
public class GetBlockchainTransactionsQueryHandler
    : IRequestHandler<GetBlockchainTransactionsQuery, IReadOnlyList<BlockchainTransactionDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<GetBlockchainTransactionsQueryHandler> _logger;

    public GetBlockchainTransactionsQueryHandler(
        IUnitOfWork uow,
        ILogger<GetBlockchainTransactionsQueryHandler> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BlockchainTransactionDto>> Handle(
        GetBlockchainTransactionsQuery req, CancellationToken ct)
    {
        var list = await _uow.BlockchainTransactions.SearchAsync(
            status: req.Status,
            functionName: req.FunctionName,
            batchId: req.BatchId,
            ct: ct);

        _logger.LogInformation(
            "Admin Blockchain list: status={Status}, fn={Fn}, batch={BatchId}, returned={Count}",
            req.Status, req.FunctionName, req.BatchId, list.Count);

        return list.Select(t => new BlockchainTransactionDto(
            Id: t.Id,
            BatchId: t.BatchId,
            BatchCode: t.Batch?.BatchCode,
            SubBatchId: t.SubBatchId,
            SubBatchCode: t.SubBatch?.SubBatchCode,
            WalletAddress: t.WalletAddress,
            TransactionHash: t.TransactionHash,
            ContractAddress: t.ContractAddress,
            FunctionName: t.FunctionName,
            BlockNumber: t.BlockNumber,
            Timestamp: t.Timestamp,
            Status: t.Status.ToString(),
            ErrorMessage: t.ErrorMessage)).ToList();
    }
}
