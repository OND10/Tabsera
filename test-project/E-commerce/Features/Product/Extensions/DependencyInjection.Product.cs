// ADD THESE LINES TO YOUR DependencyInjection.cs:
// using VsaProject.Api.Products.Product.Repository;
// services.AddScoped<IProductRepository, ProductRepository>();

// EXAMPLE: Your DependencyInjection should look like this after adding the Product:
/*
using VsaProject.Api.Products.Product.Repository; // <- ADD THIS USING

namespace VsaProject.Api.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddProjectServices(this IServiceCollection services)
        {
            // ADD THIS LINE:
            services.AddScoped<IProductRepository, ProductRepository>();
            
            return services;
        }
    }
}
*/