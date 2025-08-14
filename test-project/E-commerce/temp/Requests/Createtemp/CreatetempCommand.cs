using VsaProject.Api.Common.Handler;
using VsaProject.Api.Products.Product.Dtos.Request;
using VsaProject.Api.Products.Product.Dtos.Response;

namespace VsaProject.Api.Products.Product.Requests.CreateProduct;

public record CreateProductCommand(AddProductRequestDto Request) : ICommand<Result<ProductResponseDto>>;
