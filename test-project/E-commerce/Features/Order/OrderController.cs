using Microsoft.AspNetCore.Mvc;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Orders.Order.Dtos.Request;
using VsaProject.Api.Orders.Order.Dtos.Response;
using VsaProject.Api.Orders.Order.Repository;
using VsaProject.Api.Orders.Order.Requests.CreateOrder;

namespace VsaProject.Api.Orders.Order;

[ApiController]
[Route("api/[controller]")]
public class OrderController(IOrderRepository repo) : ControllerBase
{
    [HttpPost("create")]
    public async Task<ActionResult<Result<OrderResponseDto>>> Create([FromBody] AddOrderRequestDto dto, CancellationToken ct)
    {
        var handler = new CreateOrderCommandHandler(repo);
        var result = await handler.Handle(new CreateOrderCommand(dto), ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
