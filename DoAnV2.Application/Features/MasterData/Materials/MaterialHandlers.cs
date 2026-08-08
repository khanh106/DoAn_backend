using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Application.Features.MasterData.FruitTypes;
using DoAnV2.Domain.Entities;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.Materials;

public class CreateMaterialCommandHandler : IRequestHandler<CreateMaterialCommand, MaterialItemDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public CreateMaterialCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<MaterialItemDto> Handle(CreateMaterialCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var entity = new MaterialItem
        {
            ProcessorId = processorId,
            ItemType = req.ItemType,
            Code = req.Code.Trim().ToUpperInvariant(),
            Name = req.Name.Trim(),
            Unit = req.Unit.Trim(),
            Price = req.Price,
            QuantityInStock = 0,
            DosagePerHa = req.DosagePerHa,
            Concentration = req.Concentration,
            Supplier = req.Supplier,
            NPKRatio = req.NPKRatio,
            Note = req.Note,
        };

        await _uow.MaterialItems.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return Map(entity);
    }

    private static MaterialItemDto Map(MaterialItem e) =>
        new(e.Id, e.ItemType, e.Code, e.Name, e.Unit, e.Price, e.QuantityInStock,
            e.DosagePerHa, e.Concentration, e.Supplier, e.NPKRatio, e.Note);
}

public class UpdateMaterialCommandHandler : IRequestHandler<UpdateMaterialCommand, MaterialItemDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public UpdateMaterialCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<MaterialItemDto> Handle(UpdateMaterialCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var entity = await _uow.MaterialItems.GetByIdAsync(req.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy MaterialItem {req.Id}.");

        if (entity.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền sửa vật tư của Processor khác.");

        if (!string.IsNullOrWhiteSpace(req.Name)) entity.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.Unit)) entity.Unit = req.Unit.Trim();
        if (req.Price.HasValue) entity.Price = req.Price.Value;
        if (req.DosagePerHa.HasValue) entity.DosagePerHa = req.DosagePerHa;
        if (req.Concentration.HasValue) entity.Concentration = req.Concentration;
        if (req.Supplier is not null) entity.Supplier = req.Supplier;
        if (req.NPKRatio is not null) entity.NPKRatio = req.NPKRatio;
        if (req.Note is not null) entity.Note = req.Note;

        _uow.MaterialItems.Update(entity);
        await _uow.SaveChangesAsync(ct);

        return new MaterialItemDto(
            entity.Id, entity.ItemType, entity.Code, entity.Name, entity.Unit, entity.Price,
            entity.QuantityInStock, entity.DosagePerHa, entity.Concentration,
            entity.Supplier, entity.NPKRatio, entity.Note);
    }
}

public class GetMaterialsQueryHandler : IRequestHandler<GetMaterialsQuery, IReadOnlyList<MaterialItemDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetMaterialsQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MaterialItemDto>> Handle(GetMaterialsQuery req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);
        var list = await _uow.MaterialItems.GetByProcessorIdAsync(processorId, ct);
        return list.Select(e => new MaterialItemDto(
            e.Id, e.ItemType, e.Code, e.Name, e.Unit, e.Price, e.QuantityInStock,
            e.DosagePerHa, e.Concentration, e.Supplier, e.NPKRatio, e.Note)).ToList();
    }
}