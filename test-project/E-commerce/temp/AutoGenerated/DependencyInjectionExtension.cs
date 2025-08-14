using VsaProject.Api.Products.Product.Repository;

namespace VsaProject.Api.Extensions;

// IMPORTANT: This is an extension method for DependencyInjection
// Call this method from your main DependencyInjection.AddProjectServices() method
public static class ProductDependencyInjection
{
    public static IServiceCollection AddProductServices(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        
        return services;
    }
}