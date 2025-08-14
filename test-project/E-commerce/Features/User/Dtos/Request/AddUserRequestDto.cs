namespace VsaProject.Api.Users.User.Dtos.Request;

public class AddUserRequestDto
{
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
}
