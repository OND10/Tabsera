namespace VsaProject.Api.Products.Product.Dtos.Response;

public class ProductResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
}