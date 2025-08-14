using Microsoft.EntityFrameworkCore;
using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
    }
}
