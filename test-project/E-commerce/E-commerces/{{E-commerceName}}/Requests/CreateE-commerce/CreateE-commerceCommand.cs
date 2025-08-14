using VsaProject.Api.Abstractions.Messaging;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Products.Product.Dtos.Request;
using VsaProject.Api.Products.Product.Dtos.Response;

namespace VsaProject.Api.Products.Product.Requests.CreateProduct;

public class CreateProductCommand(AddProductRequestDto dto) : ICommand<Result<ProductResponseDto>>
{
    public AddProductRequestDto Dto { get; } = dto;
}
