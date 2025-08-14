using Microsoft.AspNetCore.Mvc;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Products.Product.Dtos.Request;
using VsaProject.Api.Products.Product.Dtos.Response;
using VsaProject.Api.Products.Product.Repository;
using VsaProject.Api.Products.Product.Requests.CreateProduct;

namespace VsaProject.Api.Products.Product;

[ApiController]
[Route("api/[controller]")]
public class ProductController(IProductRepository repository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Result<ProductResponseDto>>> Create([FromBody] AddProductRequestDto dto, CancellationToken ct)
    {
        var handler = new CreateProductCommandHandler(repository);
        var result = await handler.Handle(new CreateProductCommand(dto), ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(id, ct);
        if (product == null)
            return NotFound($"Product with ID {id} not found.");

        var response = new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            IsActive = product.IsActive
        };

        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetAll(CancellationToken ct)
    {
        var products = await repository.GetAllAsync(ct);
        var response = products.Select(f => new ProductResponseDto
        {
            Id = f.Id,
            Name = f.Name,
            Description = f.Description,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt,
            IsActive = f.IsActive
        });

        return Ok(response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductResponseDto>> Update(Guid id, [FromBody] UpdateProductRequestDto dto, CancellationToken ct)
    {
        var existingProduct = await repository.GetByIdAsync(id, ct);
        if (existingProduct == null)
            return NotFound($"Product with ID {id} not found.");

        existingProduct.Name = dto.Name;
        existingProduct.Description = dto.Description;
        existingProduct.IsActive = dto.IsActive;
        existingProduct.UpdatedAt = DateTime.UtcNow;

        var updatedProduct = await repository.UpdateAsync(existingProduct, ct);

        var response = new ProductResponseDto
        {
            Id = updatedProduct.Id,
            Name = updatedProduct.Name,
            Description = updatedProduct.Description,
            CreatedAt = updatedProduct.CreatedAt,
            UpdatedAt = updatedProduct.UpdatedAt,
            IsActive = updatedProduct.IsActive
        };

        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await repository.DeleteAsync(id, ct);
        if (!success)
            return NotFound($"Product with ID {id} not found.");

        return NoContent();
    }
}
