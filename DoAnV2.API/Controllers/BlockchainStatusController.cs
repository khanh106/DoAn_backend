using DoAnV2.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/processor/batches/{batchId:guid}")]
[Authorize(Roles = "PROCESSOR,ADMIN")]
public class BlockchainStatusController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public BlockchainStatusController(IUnitOfWork uow) => _uow = uow;

    /// <summary>
    /// Frontend polling mỗi 2-3 giây để biết khi nào batch được đồng bộ on-chain.
    /// </summary>
    [HttpGet("blockchain-status")]
    public async Task<IActionResult> GetStatus(Guid batchId, CancellationToken ct)
    {
        var batch = await _uow.Batches.GetByIdAsync(batchId, ct);
        if (batch is null)
            return NotFound(new { message = "Batch không tồn tại." });

        return Ok(new
        {
            batchId = batch.Id,
            blockchainSyncStatus = batch.BlockchainSyncStatus.ToString(),
            createBatchTxHash = batch.CreateBatchTxHash,
            blockchainSyncedAt = batch.BlockchainSyncedAt,
            blockchainSyncError = batch.BlockchainSyncError,
        });
    }
}