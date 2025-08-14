namespace VsaProject.Api.Products.Product.Dtos.Request;

public class UpdateProductRequestDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
}
