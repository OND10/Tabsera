// ADD THIS LINE TO YOUR AppDbContext.cs:
// public DbSet<Product> Products => Set<Product>();

// EXAMPLE: Your AppDbContext should look like this after adding the Product:
/*
using Microsoft.EntityFrameworkCore;
using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Product> Products => Set<Product>(); // <- ADD THIS LINE
    }
}
*/