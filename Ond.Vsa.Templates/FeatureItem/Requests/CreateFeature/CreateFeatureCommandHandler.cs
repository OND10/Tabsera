using VsaProject.Api.Abstractions.Messaging;
using VsaProject.Api.Common.Handler;
using VsaProject.Api.Common.Validations;
using VsaProject.Api.Data.Entities;
using VsaProject.Api.Features.Feature.Dtos.Response;
using VsaProject.Api.Features.Feature.Repository;

namespace VsaProject.Api.Features.Feature.Requests.CreateFeature;

public class CreateFeatureCommandHandler(IFeatureRepository repo)
    : ICommandHandler<CreateFeatureCommand, Result<FeatureResponseDto>>
{
    public async Task<Result<FeatureResponseDto>> Handle(CreateFeatureCommand command, CancellationToken ct = default)
    {
        command.Dto.Email.ThrowIfNullOrEmpty("Email");
        command.Dto.FullName.ThrowIfNullOrEmpty("FullName");

        var entity = new User { Email = command.Dto.Email, FullName = command.Dto.FullName };
        entity = await repo.AddAsync(entity, ct);

        var response = new FeatureResponseDto
        {
            Id = entity.Id,
            Email = entity.Email,
            FullName = entity.FullName
        };

        return await Result<FeatureResponseDto>.SuccessAsync(response, "Feature is created successfully", true);
    }
}
