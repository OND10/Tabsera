using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Products.Product.Repository;

public interface IProductRepository
{
    Task<Product> AddAsync(Product temp, CancellationToken ct = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product> UpdateAsync(Product temp, CancellationToken ct = default);
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
