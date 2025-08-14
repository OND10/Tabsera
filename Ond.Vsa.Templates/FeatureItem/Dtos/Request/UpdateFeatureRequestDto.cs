namespace VsaProject.Api.Features.Feature.Dtos.Request;

public class UpdateFeatureRequestDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
}
