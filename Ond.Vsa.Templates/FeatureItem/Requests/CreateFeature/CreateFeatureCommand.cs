using VsaProject.Api.Abstractions.Messaging;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Features.Feature.Dtos.Request;
using VsaProject.Api.Features.Feature.Dtos.Response;

namespace VsaProject.Api.Features.Feature.Requests.CreateFeature;

public class CreateFeatureCommand(AddFeatureRequestDto dto) : ICommand<Result<FeatureResponseDto>>
{
    public AddFeatureRequestDto Dto { get; } = dto;
}
