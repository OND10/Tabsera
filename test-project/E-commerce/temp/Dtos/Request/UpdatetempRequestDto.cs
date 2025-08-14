namespace VsaProject.Api.Products.Product.Dtos.Request;

public class UpdateProductRequestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
