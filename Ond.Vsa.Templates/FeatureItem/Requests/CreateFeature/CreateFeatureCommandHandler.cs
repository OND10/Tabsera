using VsaProject.Api.Common.Handler;
using VsaProject.Api.Data.Entities;
using VsaProject.Api.Features.Feature.Dtos.Response;
using VsaProject.Api.Features.Feature.Repository;

namespace VsaProject.Api.Features.Feature.Requests.CreateFeature;

public class CreateFeatureCommandHandler(IFeatureRepository repository) : ICommandHandler<CreateFeatureCommand, Result<FeatureResponseDto>>
{
    public async Task<Result<FeatureResponseDto>> Handle(CreateFeatureCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var feature = new Data.Entities.Feature
            {
                Id = Guid.NewGuid(),
                Name = request.Request.Name,
                Description = request.Request.Description,
                IsActive = request.Request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            var createdFeature = await repository.AddAsync(feature, cancellationToken);

            var response = new FeatureResponseDto
            {
                Id = createdFeature.Id,
                Name = createdFeature.Name,
                Description = createdFeature.Description,
                CreatedAt = createdFeature.CreatedAt,
                UpdatedAt = createdFeature.UpdatedAt,
                IsActive = createdFeature.IsActive
            };

            return Result<FeatureResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<FeatureResponseDto>.Failure($"Failed to create feature: {ex.Message}");
        }
    }
}
