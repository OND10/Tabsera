using Microsoft.EntityFrameworkCore;
using VsaProject.Api.Data;
using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Features.Feature.Repository;

public class FeatureRepository(AppDbContext db) : IFeatureRepository
{
    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User> UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
