using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Application.Features.MasterData.FruitTypes;
using DoAnV2.Domain.Entities;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.Products;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public CreateProductCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<ProductDto> Handle(CreateProductCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var fruitType = await _uow.FruitTypes.GetByIdAsync(req.FruitTypeId, ct)
            ?? throw new NotFoundException($"Không tìm thấy FruitType {req.FruitTypeId}.");

        if (fruitType.ProcessorId != processorId)
            throw new ForbiddenException("FruitType này không thuộc Processor của bạn.");

        var entity = new Product
        {
            FruitTypeId = fruitType.Id,
            GroupName = string.IsNullOrWhiteSpace(req.GroupName) ? "Trái cây tươi đóng gói" : req.GroupName.Trim(),
            ProductType = string.IsNullOrWhiteSpace(req.ProductType) ? "FRESH" : req.ProductType.Trim(),
            Variety = string.IsNullOrWhiteSpace(req.Variety) ? (fruitType.Name ?? "N/A") : req.Variety.Trim(),
            Name = req.Name.Trim(),
            ShortName = string.IsNullOrWhiteSpace(req.ShortName) ? req.Name.Trim() : req.ShortName.Trim(),
            Description = req.Description,
            Status = "ACTIVE",
        };

        await _uow.Products.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return new ProductDto(
            entity.Id, entity.FruitTypeId, fruitType.Name,
            entity.GroupName, entity.ProductType, entity.Variety, entity.Name, entity.ShortName,
            entity.Description, entity.Status, entity.CreatedAt);
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public UpdateProductCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var entity = await _uow.Products.GetByIdAsync(req.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy Product {req.Id}.");

        var fruitType = await _uow.FruitTypes.GetByIdAsync(entity.FruitTypeId, ct)
            ?? throw new NotFoundException("FruitType liên kết không tồn tại.");

        if (fruitType.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền sửa Product của Processor khác.");

        if (!string.IsNullOrWhiteSpace(req.GroupName)) entity.GroupName = req.GroupName.Trim();
        if (!string.IsNullOrWhiteSpace(req.ProductType)) entity.ProductType = req.ProductType.Trim();
        if (!string.IsNullOrWhiteSpace(req.Variety)) entity.Variety = req.Variety.Trim();
        if (!string.IsNullOrWhiteSpace(req.Name)) entity.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.ShortName)) entity.ShortName = req.ShortName.Trim();
        if (req.Description is not null) entity.Description = req.Description;
        if (!string.IsNullOrWhiteSpace(req.Status)) entity.Status = req.Status;

        _uow.Products.Update(entity);
        await _uow.SaveChangesAsync(ct);

        return new ProductDto(
            entity.Id, entity.FruitTypeId, fruitType.Name,
            entity.GroupName, entity.ProductType, entity.Variety, entity.Name, entity.ShortName,
            entity.Description, entity.Status, entity.CreatedAt);
    }
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetProductsQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);
        var list = await _uow.Products.GetByProcessorIdAsync(processorId, ct);
        return list.Select(p => new ProductDto(
            p.Id, p.FruitTypeId, p.FruitType.Name,
            p.GroupName, p.ProductType, p.Variety, p.Name, p.ShortName,
            p.Description, p.Status, p.CreatedAt)).ToList();
    }
}