using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Features.Feature.Repository;

public interface IFeatureRepository
{
    Task<Feature> AddAsync(Feature feature, CancellationToken ct = default);
    Task<Feature?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Feature> UpdateAsync(Feature feature, CancellationToken ct = default);
    Task<IEnumerable<Feature>> GetAllAsync(CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
