using VsaProject.Api.Abstractions.Messaging;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Common.Validations;
using VsaProject.Api.Data.Entities;
using VsaProject.Api.Products.Product.Dtos.Response;
using VsaProject.Api.Products.Product.Repository;

namespace VsaProject.Api.Products.Product.Requests.CreateProduct;

public class CreateProductCommandHandler(IProductRepository repo)
    : ICommandHandler<CreateProductCommand, Result<ProductResponseDto>>
{
    public async Task<Result<ProductResponseDto>> Handle(CreateProductCommand command, CancellationToken ct = default)
    {
        command.Dto.Email.ThrowIfNullOrEmpty("Email");
        command.Dto.FullName.ThrowIfNullOrEmpty("FullName");

        var entity = new User { Email = command.Dto.Email, FullName = command.Dto.FullName };
        entity = await repo.AddAsync(entity, ct);

        var response = new ProductResponseDto
        {
            Id = entity.Id,
            Email = entity.Email,
            FullName = entity.FullName
        };

        return await Result<ProductResponseDto>.SuccessAsync(response, "Product is created successfully", true);
    }
}
