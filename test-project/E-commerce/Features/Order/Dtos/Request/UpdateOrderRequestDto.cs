namespace VsaProject.Api.Orders.Order.Dtos.Request;

public class UpdateOrderRequestDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
}
