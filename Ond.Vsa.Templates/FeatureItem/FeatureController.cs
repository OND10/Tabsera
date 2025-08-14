using Microsoft.AspNetCore.Mvc;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Features.Feature.Dtos.Request;
using VsaProject.Api.Features.Feature.Dtos.Response;
using VsaProject.Api.Features.Feature.Repository;
using VsaProject.Api.Features.Feature.Requests.CreateFeature;

namespace VsaProject.Api.Features.Feature;

[ApiController]
[Route("api/[controller]")]
public class FeatureController(IFeatureRepository repository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Result<FeatureResponseDto>>> Create([FromBody] AddFeatureRequestDto dto, CancellationToken ct)
    {
        var handler = new CreateFeatureCommandHandler(repository);
        var result = await handler.Handle(new CreateFeatureCommand(dto), ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FeatureResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var feature = await repository.GetByIdAsync(id, ct);
        if (feature == null)
            return NotFound($"Feature with ID {id} not found.");

        var response = new FeatureResponseDto
        {
            Id = feature.Id,
            Name = feature.Name,
            Description = feature.Description,
            CreatedAt = feature.CreatedAt,
            UpdatedAt = feature.UpdatedAt,
            IsActive = feature.IsActive
        };

        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FeatureResponseDto>>> GetAll(CancellationToken ct)
    {
        var features = await repository.GetAllAsync(ct);
        var response = features.Select(f => new FeatureResponseDto
        {
            Id = f.Id,
            Name = f.Name,
            Description = f.Description,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt,
            IsActive = f.IsActive
        });

        return Ok(response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FeatureResponseDto>> Update(Guid id, [FromBody] UpdateFeatureRequestDto dto, CancellationToken ct)
    {
        var existingFeature = await repository.GetByIdAsync(id, ct);
        if (existingFeature == null)
            return NotFound($"Feature with ID {id} not found.");

        existingFeature.Name = dto.Name;
        existingFeature.Description = dto.Description;
        existingFeature.IsActive = dto.IsActive;
        existingFeature.UpdatedAt = DateTime.UtcNow;

        var updatedFeature = await repository.UpdateAsync(existingFeature, ct);

        var response = new FeatureResponseDto
        {
            Id = updatedFeature.Id,
            Name = updatedFeature.Name,
            Description = updatedFeature.Description,
            CreatedAt = updatedFeature.CreatedAt,
            UpdatedAt = updatedFeature.UpdatedAt,
            IsActive = updatedFeature.IsActive
        };

        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await repository.DeleteAsync(id, ct);
        if (!success)
            return NotFound($"Feature with ID {id} not found.");

        return NoContent();
    }
}
