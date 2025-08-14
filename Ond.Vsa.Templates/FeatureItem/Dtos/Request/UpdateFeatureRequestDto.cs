namespace VsaProject.Api.Features.Feature.Dtos.Request;

public class UpdateFeatureRequestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
