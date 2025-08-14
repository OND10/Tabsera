using VsaProject.Api.Abstractions.Messaging;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Orders.Order.Dtos.Request;
using VsaProject.Api.Orders.Order.Dtos.Response;

namespace VsaProject.Api.Orders.Order.Requests.CreateOrder;

public class CreateOrderCommand(AddOrderRequestDto dto) : ICommand<Result<OrderResponseDto>>
{
    public AddOrderRequestDto Dto { get; } = dto;
}
