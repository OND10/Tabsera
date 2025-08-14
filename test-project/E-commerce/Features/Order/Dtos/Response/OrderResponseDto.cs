namespace VsaProject.Api.Orders.Order.Dtos.Response;

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
}