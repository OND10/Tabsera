using VsaProject.Api.Abstractions.Messaging;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Users.User.Dtos.Request;
using VsaProject.Api.Users.User.Dtos.Response;

namespace VsaProject.Api.Users.User.Requests.CreateUser;

public class CreateUserCommand(AddUserRequestDto dto) : ICommand<Result<UserResponseDto>>
{
    public AddUserRequestDto Dto { get; } = dto;
}
