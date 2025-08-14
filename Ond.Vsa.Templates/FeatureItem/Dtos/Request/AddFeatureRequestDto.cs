namespace VsaProject.Api.Features.Feature.Dtos.Request;

public class AddFeatureRequestDto
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
