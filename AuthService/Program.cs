using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Helpers;
using AuthService.Repositories;
using AuthService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAngularCors(builder.Configuration);
builder.Services.AddDbContext<ServicesDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ServicesDb")));
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();
builder.Services.AddSingleton<JwtHelper>();

builder.Services.AddSharedControllers();
builder.Services.AddSwaggerWithJwt(
    title:       "AuthService API",
    description: "Authentication & session management for LifeSci360");

var app = builder.Build();

// Ensure SYSTEM_ADMIN role exists and the seeded system admin user has it
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();

    var systemAdminRole = db.Roles.FirstOrDefault(r => r.RoleName == "SYSTEM_ADMIN");
    if (systemAdminRole == null)
    {
        systemAdminRole = new Role { RoleName = "SYSTEM_ADMIN", IsActive = true };
        db.Roles.Add(systemAdminRole);
        db.SaveChanges();
    }

    var systemAdminUser = db.Users.FirstOrDefault(u => u.Email == "admin@lifesci360.com");
    if (systemAdminUser != null && systemAdminUser.RoleId != systemAdminRole.RoleId)
    {
        systemAdminUser.RoleId = systemAdminRole.RoleId;
        db.SaveChanges();
    }
}

app.UseSwaggerInDevelopment("AuthService v1");
app.UseSharedMiddleware();
app.MapControllers();
app.Run();
