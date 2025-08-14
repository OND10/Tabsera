using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Orders.Order.Repository;

public interface IOrderRepository
{
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User> UpdateAsync(User user, CancellationToken ct = default);
}
