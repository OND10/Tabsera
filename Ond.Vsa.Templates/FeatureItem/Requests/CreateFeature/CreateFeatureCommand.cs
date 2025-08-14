using VsaProject.Api.Common.Handler;
using VsaProject.Api.Features.Feature.Dtos.Request;
using VsaProject.Api.Features.Feature.Dtos.Response;

namespace VsaProject.Api.Features.Feature.Requests.CreateFeature;

public record CreateFeatureCommand(AddFeatureRequestDto Request) : ICommand<Result<FeatureResponseDto>>;
