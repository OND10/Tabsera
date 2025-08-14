namespace VsaProject.Api.Users.User.Dtos.Request;

public class UpdateUserRequestDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
}
