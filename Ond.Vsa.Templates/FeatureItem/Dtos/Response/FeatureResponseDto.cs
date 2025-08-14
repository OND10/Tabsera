namespace VsaProject.Api.Features.Feature.Dtos.Response;

public class FeatureResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
}