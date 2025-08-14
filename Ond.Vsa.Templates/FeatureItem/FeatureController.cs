using Microsoft.AspNetCore.Mvc;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Features.Feature.Dtos.Request;
using VsaProject.Api.Features.Feature.Dtos.Response;
using VsaProject.Api.Features.Feature.Repository;
using VsaProject.Api.Features.Feature.Requests.CreateFeature;

namespace VsaProject.Api.Features.Feature;

[ApiController]
[Route("api/[controller]")]
public class FeatureController(IFeatureRepository repo) : ControllerBase
{
    [HttpPost("create")]
    public async Task<ActionResult<Result<FeatureResponseDto>>> Create([FromBody] AddFeatureRequestDto dto, CancellationToken ct)
    {
        var handler = new CreateFeatureCommandHandler(repo);
        var result = await handler.Handle(new CreateFeatureCommand(dto), ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
