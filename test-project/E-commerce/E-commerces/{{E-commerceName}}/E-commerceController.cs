using Microsoft.AspNetCore.Mvc;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Products.Product.Dtos.Request;
using VsaProject.Api.Products.Product.Dtos.Response;
using VsaProject.Api.Products.Product.Repository;
using VsaProject.Api.Products.Product.Requests.CreateProduct;

namespace VsaProject.Api.Products.Product;

[ApiController]
[Route("api/[controller]")]
public class ProductController(IProductRepository repo) : ControllerBase
{
    [HttpPost("create")]
    public async Task<ActionResult<Result<ProductResponseDto>>> Create([FromBody] AddProductRequestDto dto, CancellationToken ct)
    {
        var handler = new CreateProductCommandHandler(repo);
        var result = await handler.Handle(new CreateProductCommand(dto), ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
