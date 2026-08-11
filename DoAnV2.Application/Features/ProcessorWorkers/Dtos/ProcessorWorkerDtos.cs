namespace DoAnV2.Application.Features.ProcessorWorkers.Dtos;

public record SearchWorkerResultDto(
    Guid WorkerId,
    string FullName,
    string Email,
    string Phone,
    string? WalletAddress,
    string LinkStatus // "NONE", "PENDING", "ACCEPTED", "REJECTED"
);

public record ProcessorWorkerLinkDto(
    Guid Id,
    Guid ProcessorId,
    string ProcessorName,
    Guid WorkerId,
    string WorkerName,
    string WorkerEmail,
    string WorkerPhone,
    string? WorkerWalletAddress,
    string Status,
    DateTime InvitedAt,
    DateTime? RespondedAt
);

public record SendInvitationRequest(Guid WorkerId);
public record RespondInvitationRequest(string Action); // "ACCEPT" hoặc "REJECT"
