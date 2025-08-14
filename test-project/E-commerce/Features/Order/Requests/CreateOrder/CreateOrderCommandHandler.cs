using VsaProject.Api.Abstractions.Messaging;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Common.Validations;
using VsaProject.Api.Data.Entities;
using VsaProject.Api.Orders.Order.Dtos.Response;
using VsaProject.Api.Orders.Order.Repository;

namespace VsaProject.Api.Orders.Order.Requests.CreateOrder;

public class CreateOrderCommandHandler(IOrderRepository repo)
    : ICommandHandler<CreateOrderCommand, Result<OrderResponseDto>>
{
    public async Task<Result<OrderResponseDto>> Handle(CreateOrderCommand command, CancellationToken ct = default)
    {
        command.Dto.Email.ThrowIfNullOrEmpty("Email");
        command.Dto.FullName.ThrowIfNullOrEmpty("FullName");

        var entity = new User { Email = command.Dto.Email, FullName = command.Dto.FullName };
        entity = await repo.AddAsync(entity, ct);

        var response = new OrderResponseDto
        {
            Id = entity.Id,
            Email = entity.Email,
            FullName = entity.FullName
        };

        return await Result<OrderResponseDto>.SuccessAsync(response, "Order is created successfully", true);
    }
}
