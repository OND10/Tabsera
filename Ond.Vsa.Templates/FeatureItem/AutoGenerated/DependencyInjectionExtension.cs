using VsaProject.Api.Features.Feature.Repository;

namespace VsaProject.Api.Extensions;

// IMPORTANT: This is an extension method for DependencyInjection
// Call this method from your main DependencyInjection.AddProjectServices() method
public static class FeatureDependencyInjection
{
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddScoped<IFeatureRepository, FeatureRepository>();
        
        return services;
    }
}