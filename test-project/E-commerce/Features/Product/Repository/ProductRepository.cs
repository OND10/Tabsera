using Microsoft.EntityFrameworkCore;
using VsaProject.Api.Data;
using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Products.Product.Repository;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Products.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<Product> UpdateAsync(Product product, CancellationToken ct = default)
    {
        db.Products.Update(product);
        await db.SaveChangesAsync(ct);
        return product;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default) =>
        await db.Products.Where(f => f.IsActive).ToListAsync(ct);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await GetByIdAsync(id, ct);
        if (product == null) return false;
        
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
