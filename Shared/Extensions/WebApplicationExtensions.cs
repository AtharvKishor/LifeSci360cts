using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Shared.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Applies the Angular CORS policy, then authentication and authorization middleware
    /// in the correct pipeline order.
    /// </summary>
    public static WebApplication UseSharedMiddleware(this WebApplication app)
    {
        app.UseCors("Angular");
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    /// <summary>
    /// Enables Swagger UI in the Development environment.
    /// </summary>
    public static WebApplication UseSwaggerInDevelopment(
        this WebApplication app,
        string endpointTitle = "API v1",
        string routePrefix   = "swagger")
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", endpointTitle);
                options.RoutePrefix = routePrefix;
            });
        }
        return app;
    }
}
