using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Application.Features.MasterData.FruitTypes;
using DoAnV2.Domain.Entities;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.FarmAreas;

public class CreateFarmAreaCommandHandler : IRequestHandler<CreateFarmAreaCommand, FarmAreaDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public CreateFarmAreaCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<FarmAreaDto> Handle(CreateFarmAreaCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        if (req.Area <= 0)
            throw new ValidationException("Diện tích vùng trồng phải > 0.");

        var entity = new FarmArea
        {
            ProcessorId = processorId,
            Name = req.Name.Trim(),
            OwnerName = req.OwnerName.Trim(),
            Province = req.Province.Trim(),
            District = req.District.Trim(),
            Ward = req.Ward.Trim(),
            Area = req.Area,
            SoilType = req.SoilType,
            GPS = req.GPS,
            PlantingCode = req.PlantingCode?.Trim(),
        };

        await _uow.FarmAreas.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return Map(entity);
    }

    private static FarmAreaDto Map(FarmArea e) =>
        new(e.Id, e.Name, e.OwnerName, e.Province, e.District, e.Ward,
            e.Area, e.SoilType, e.GPS, e.PlantingCode, e.CreatedAt, e.UpdatedAt);
}

public class UpdateFarmAreaCommandHandler : IRequestHandler<UpdateFarmAreaCommand, FarmAreaDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public UpdateFarmAreaCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<FarmAreaDto> Handle(UpdateFarmAreaCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var entity = await _uow.FarmAreas.GetByIdAsync(req.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy FarmArea {req.Id}.");

        if (entity.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền sửa vùng trồng của Processor khác.");

        if (!string.IsNullOrWhiteSpace(req.Name)) entity.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.OwnerName)) entity.OwnerName = req.OwnerName.Trim();
        if (!string.IsNullOrWhiteSpace(req.Province)) entity.Province = req.Province.Trim();
        if (!string.IsNullOrWhiteSpace(req.District)) entity.District = req.District.Trim();
        if (!string.IsNullOrWhiteSpace(req.Ward)) entity.Ward = req.Ward.Trim();
        if (req.Area.HasValue)
        {
            if (req.Area.Value <= 0)
                throw new ValidationException("Diện tích vùng trồng phải > 0.");
            entity.Area = req.Area.Value;
        }
        if (req.SoilType is not null) entity.SoilType = req.SoilType;
        if (req.GPS is not null) entity.GPS = req.GPS;
        if (req.PlantingCode is not null) entity.PlantingCode = req.PlantingCode.Trim();

        _uow.FarmAreas.Update(entity);
        await _uow.SaveChangesAsync(ct);

        return new FarmAreaDto(
            entity.Id, entity.Name, entity.OwnerName, entity.Province, entity.District,
            entity.Ward, entity.Area, entity.SoilType, entity.GPS, entity.PlantingCode,
            entity.CreatedAt, entity.UpdatedAt);
    }
}

public class GetFarmAreasQueryHandler : IRequestHandler<GetFarmAreasQuery, IReadOnlyList<FarmAreaDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetFarmAreasQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FarmAreaDto>> Handle(GetFarmAreasQuery req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);
        var list = await _uow.FarmAreas.GetByProcessorIdAsync(
            processorId, req.Province, req.District, req.Ward, req.PlantingCode, ct);

        return list.Select(e => new FarmAreaDto(
            e.Id, e.Name, e.OwnerName, e.Province, e.District, e.Ward,
            e.Area, e.SoilType, e.GPS, e.PlantingCode, e.CreatedAt, e.UpdatedAt)).ToList();
    }
}

public class GetFarmAreaByIdQueryHandler : IRequestHandler<GetFarmAreaByIdQuery, FarmAreaDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetFarmAreaByIdQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<FarmAreaDto> Handle(GetFarmAreaByIdQuery req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);
        var entity = await _uow.FarmAreas.GetByIdAsync(req.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy FarmArea {req.Id}.");

        if (entity.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền xem vùng trồng của Processor khác.");

        return new FarmAreaDto(
            entity.Id, entity.Name, entity.OwnerName, entity.Province, entity.District,
            entity.Ward, entity.Area, entity.SoilType, entity.GPS, entity.PlantingCode,
            entity.CreatedAt, entity.UpdatedAt);
    }
}

// 👈 Dán toàn bộ class bên dưới vào cuối file FarmAreaHandlers.cs:

public class DeleteFarmAreaCommandHandler : IRequestHandler<DeleteFarmAreaCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public DeleteFarmAreaCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(DeleteFarmAreaCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var entity = await _uow.FarmAreas.GetByIdAsync(req.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy vùng trồng {req.Id}.");

        if (entity.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền xóa vùng trồng của HTX/Doanh nghiệp khác.");

        _uow.FarmAreas.Delete(entity);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
