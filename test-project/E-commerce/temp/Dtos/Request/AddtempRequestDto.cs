namespace VsaProject.Api.Products.Product.Dtos.Request;

public class AddProductRequestDto
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
