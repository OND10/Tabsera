using Microsoft.EntityFrameworkCore;
using E_commerce.Api.Data.Entities;

namespace E_commerce.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
    }
}
