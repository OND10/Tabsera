using Microsoft.EntityFrameworkCore;
using VsaProject.Api.Data;
using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Products.Product.Repository;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<Product> AddAsync(Product temp, CancellationToken ct = default)
    {
        db.Products.Add(temp);
        await db.SaveChangesAsync(ct);
        return temp;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Products.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<Product> UpdateAsync(Product temp, CancellationToken ct = default)
    {
        db.Products.Update(temp);
        await db.SaveChangesAsync(ct);
        return temp;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default) =>
        await db.Products.Where(f => f.IsActive).ToListAsync(ct);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var temp = await GetByIdAsync(id, ct);
        if (temp == null) return false;
        
        temp.IsActive = false;
        temp.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
