using Microsoft.EntityFrameworkCore;
using VsaProject.Api.Data;
using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Features.Feature.Repository;

public class FeatureRepository(AppDbContext db) : IFeatureRepository
{
    public async Task<Feature> AddAsync(Feature feature, CancellationToken ct = default)
    {
        db.Features.Add(feature);
        await db.SaveChangesAsync(ct);
        return feature;
    }

    public Task<Feature?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<Feature> UpdateAsync(Feature feature, CancellationToken ct = default)
    {
        db.Features.Update(feature);
        await db.SaveChangesAsync(ct);
        return feature;
    }

    public async Task<IEnumerable<Feature>> GetAllAsync(CancellationToken ct = default) =>
        await db.Features.Where(f => f.IsActive).ToListAsync(ct);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var feature = await GetByIdAsync(id, ct);
        if (feature == null) return false;
        
        feature.IsActive = false;
        feature.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
