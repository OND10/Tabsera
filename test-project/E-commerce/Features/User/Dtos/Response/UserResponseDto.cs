namespace VsaProject.Api.Users.User.Dtos.Response;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
}