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


}
