# Product Setup Instructions

After generating your Product, you need to complete these steps:

## 1. Create the Entity

Create a new file: `Data/Entities/Product.cs` in your main project with this content:

```csharp
namespace VsaProject.Api.Data.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
```

## 2. Update AppDbContext

Add this line to your `Data/AppDbContext.cs`:

```csharp
public DbSet<Product> Products => Set<Product>();
```

Your AppDbContext should look like this:
```csharp
using Microsoft.EntityFrameworkCore;
using VsaProject.Api.Data.Entities;

namespace VsaProject.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Product> Products => Set<Product>(); // <- ADD THIS
    }
}
```

## 3. Register Repository in DI

Add these lines to your `Extensions/DependencyInjection.cs`:

First, add the using statement at the top:
```csharp
using VsaProject.Api.Products.Product.Repository;
```

Then add the service registration:
```csharp
services.AddScoped<IProductRepository, ProductRepository>();
```

Your DependencyInjection should look like this:
```csharp
using VsaProject.Api.Products.Product.Repository; // <- ADD THIS

namespace VsaProject.Api.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddProjectServices(this IServiceCollection services)
        {
            services.AddScoped<IProductRepository, ProductRepository>(); // <- ADD THIS
            
            return services;
        }
    }
}
```

## 4. Run Migration (if using EF Core)

After adding the entity and DbSet, create and run a migration:

```bash
dotnet ef migrations add AddProductEntity
dotnet ef database update
```

## 5. Test Your Product

Your Product is now ready! You can test the following endpoints:

- `POST /api/Product` - Create a new temp
- `GET /api/Product` - Get all temps  
- `GET /api/Product/{id}` - Get temp by ID
- `PUT /api/Product/{id}` - Update a temp
- `DELETE /api/Product/{id}` - Delete a temp

## Sample Request

```json
POST /api/Product
{
    "name": "User Authentication",
    "description": "Implement user login and registration",
    "isActive": true
}
```