using Microsoft.EntityFrameworkCore;
using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Data;

// IMPORTANT: This is a partial class extension for AppDbContext
// It automatically adds the DbSet for Product without modifying the main AppDbContext file
public partial class AppDbContext
{
    public DbSet<Product> Products => Set<Product>();
}