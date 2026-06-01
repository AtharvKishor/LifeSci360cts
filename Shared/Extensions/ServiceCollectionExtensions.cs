using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAngularCors(
        this IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("Cors:Origins").Get<string[]>()
            ?? ["http://localhost:53719", "http://127.0.0.1:53719"];

        services.AddCors(opt =>
            opt.AddPolicy("Angular", p =>
                p.WithOrigins(origins)
                 .AllowAnyHeader()
                 .AllowAnyMethod()));

        return services;
    }

    public static IServiceCollection AddSharedControllers(this IServiceCollection services)
    {
        services.AddControllers();
        return services;
    }

    public static IServiceCollection AddSwaggerWithJwt(
        this IServiceCollection services,
        string title       = "API",
        string description = "")
    {
        // OpenApi is registered directly in each service's Program.cs
        return services;
    }

    // JWT is configured directly in each service's Program.cs
    // AddJwtAuthentication is intentionally removed to avoid version conflicts
}
