using Microsoft.AspNetCore.Mvc;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Users.User.Dtos.Request;
using VsaProject.Api.Users.User.Dtos.Response;
using VsaProject.Api.Users.User.Repository;
using VsaProject.Api.Users.User.Requests.CreateUser;

namespace VsaProject.Api.Users.User;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserRepository repo) : ControllerBase
{
    [HttpPost("create")]
    public async Task<ActionResult<Result<UserResponseDto>>> Create([FromBody] AddUserRequestDto dto, CancellationToken ct)
    {
        var handler = new CreateUserCommandHandler(repo);
        var result = await handler.Handle(new CreateUserCommand(dto), ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
