using VsaProject.Api.Common.Handler;
using VsaProject.Api.Data.Entities;
using VsaProject.Api.Products.Product.Dtos.Response;
using VsaProject.Api.Products.Product.Repository;

namespace VsaProject.Api.Products.Product.Requests.CreateProduct;

public class CreateProductCommandHandler(IProductRepository repository) : ICommandHandler<CreateProductCommand, Result<ProductResponseDto>>
{
    public async Task<Result<ProductResponseDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var temp = new Data.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = request.Request.Name,
                Description = request.Request.Description,
                IsActive = request.Request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            var createdProduct = await repository.AddAsync(temp, cancellationToken);

            var response = new ProductResponseDto
            {
                Id = createdProduct.Id,
                Name = createdProduct.Name,
                Description = createdProduct.Description,
                CreatedAt = createdProduct.CreatedAt,
                UpdatedAt = createdProduct.UpdatedAt,
                IsActive = createdProduct.IsActive
            };

            return Result<ProductResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<ProductResponseDto>.Failure($"Failed to create temp: {ex.Message}");
        }
    }
}
