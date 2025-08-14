using VsaProject.Api.Abstractions.Messaging;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Common.Validations;
using VsaProject.Api.Data.Entities;
using VsaProject.Api.Users.User.Dtos.Response;
using VsaProject.Api.Users.User.Repository;

namespace VsaProject.Api.Users.User.Requests.CreateUser;

public class CreateUserCommandHandler(IUserRepository repo)
    : ICommandHandler<CreateUserCommand, Result<UserResponseDto>>
{
    public async Task<Result<UserResponseDto>> Handle(CreateUserCommand command, CancellationToken ct = default)
    {
        command.Dto.Email.ThrowIfNullOrEmpty("Email");
        command.Dto.FullName.ThrowIfNullOrEmpty("FullName");

        var entity = new User { Email = command.Dto.Email, FullName = command.Dto.FullName };
        entity = await repo.AddAsync(entity, ct);

        var response = new UserResponseDto
        {
            Id = entity.Id,
            Email = entity.Email,
            FullName = entity.FullName
        };

        return await Result<UserResponseDto>.SuccessAsync(response, "User is created successfully", true);
    }
}
