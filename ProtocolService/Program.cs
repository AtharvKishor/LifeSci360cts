using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag.AspNetCore;
using Shared.CL;
using Shared.Extensions;
using ProtocolService.Data;
using ProtocolService.Repositories;
using ProtocolService.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ProtocolDbContext>(opts =>
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("ServicesDb"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

// JWT
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Audit Client
builder.Services.AddAuditClient(builder.Configuration);

// Repositories
builder.Services.AddScoped<IProtocolRepository, ProtocolRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<IProtocolSiteRepository, ProtocolSiteRepository>();

// Services
builder.Services.AddScoped<IProtocolService, ProtocolServiceImpl>();
builder.Services.AddScoped<ISiteService, SiteServiceImpl>();
builder.Services.AddScoped<IProtocolSiteService, ProtocolSiteServiceImpl>();

// Validation errors → ApiResponse envelope
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(opts =>
    {
        opts.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return new BadRequestObjectResult(
                ApiResponse<object>.Fail("Validation failed.", errors));
        };
    });

// OpenAPI + Swagger UI via NSwag
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "LifeSci360 — Protocol Service";
    config.Version = "v1";
    config.Description = "Module 2: Protocol registry, site management, investigator assignments.";
});

// CORS — allow any origin for development
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();