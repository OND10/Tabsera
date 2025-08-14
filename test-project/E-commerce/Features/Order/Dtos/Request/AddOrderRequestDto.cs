namespace VsaProject.Api.Orders.Order.Dtos.Request;

public class AddOrderRequestDto
{
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
}
