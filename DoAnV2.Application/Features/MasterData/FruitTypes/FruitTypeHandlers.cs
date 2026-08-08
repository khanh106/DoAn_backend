using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Domain.Entities;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.FruitTypes;

/// <summary>
/// Helper dùng chung cho FruitType handlers: ép người dùng phải là PROCESSOR đã APPROVED.
/// Trả về Guid ProcessorId (= UserId) để gắn vào FruitType.ProcessorId.
/// </summary>
internal static class ProcessorGuard
{
    public static Guid RequireProcessor(ICurrentUser current)
    {
        if (!current.IsAuthenticated || current.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var role = current.Role;
        if (!string.Equals(role, "PROCESSOR", StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Chỉ tài khoản PROCESSOR mới được phép thao tác.");

        return current.UserId.Value;
    }
}

public class CreateFruitTypeCommandHandler : IRequestHandler<CreateFruitTypeCommand, FruitTypeDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public CreateFruitTypeCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<FruitTypeDto> Handle(CreateFruitTypeCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ValidationException("Tên loại hoa quả không được trống.");
        if (string.IsNullOrWhiteSpace(req.Code))
            throw new ValidationException("Mã loại hoa quả không được trống.");

        if (await _uow.FruitTypes.CodeExistsForProcessorAsync(req.Code.ToUpperInvariant(), processorId, null, ct))
            throw new ConflictException($"Mã '{req.Code}' đã tồn tại trong danh mục của bạn.");

        var entity = new FruitType
        {
            ProcessorId = processorId,
            Name = req.Name.Trim(),
            Code = req.Code.Trim().ToUpperInvariant(),
            Description = req.Description,
            Status = "ACTIVE",
        };

        await _uow.FruitTypes.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return new FruitTypeDto(
            entity.Id, entity.Name, entity.Code, entity.Description, entity.Status,
            entity.CreatedAt, entity.UpdatedAt);
    }
}

public class UpdateFruitTypeCommandHandler : IRequestHandler<UpdateFruitTypeCommand, FruitTypeDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public UpdateFruitTypeCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<FruitTypeDto> Handle(UpdateFruitTypeCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var entity = await _uow.FruitTypes.GetByIdAsync(req.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy FruitType {req.Id}.");

        if (entity.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền sửa danh mục của Processor khác.");

        if (!string.IsNullOrWhiteSpace(req.Code) &&
            !string.Equals(req.Code, entity.Code, StringComparison.OrdinalIgnoreCase))
        {
            var newCode = req.Code.Trim().ToUpperInvariant();
            if (await _uow.FruitTypes.CodeExistsForProcessorAsync(newCode, processorId, entity.Id, ct))
                throw new ConflictException($"Mã '{newCode}' đã tồn tại.");
            entity.Code = newCode;
        }

        if (!string.IsNullOrWhiteSpace(req.Name)) entity.Name = req.Name.Trim();
        if (req.Description is not null) entity.Description = req.Description;
        if (!string.IsNullOrWhiteSpace(req.Status)) entity.Status = req.Status;

        _uow.FruitTypes.Update(entity);
        await _uow.SaveChangesAsync(ct);

        return new FruitTypeDto(
            entity.Id, entity.Name, entity.Code, entity.Description, entity.Status,
            entity.CreatedAt, entity.UpdatedAt);
    }
}

public class GetFruitTypesQueryHandler : IRequestHandler<GetFruitTypesQuery, IReadOnlyList<FruitTypeDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetFruitTypesQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FruitTypeDto>> Handle(GetFruitTypesQuery req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);
        var list = await _uow.FruitTypes.GetByProcessorIdAsync(processorId, ct);
        return list.Select(x => new FruitTypeDto(
            x.Id, x.Name, x.Code, x.Description, x.Status,
            x.CreatedAt, x.UpdatedAt)).ToList();
    }
}