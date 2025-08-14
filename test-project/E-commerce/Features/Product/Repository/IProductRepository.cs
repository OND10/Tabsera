using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Products.Product.Repository;

public interface IProductRepository
{
    Task<Product> AddAsync(Product product, CancellationToken ct = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product> UpdateAsync(Product product, CancellationToken ct = default);
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
