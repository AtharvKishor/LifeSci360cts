using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag.AspNetCore;
using Shared.CL;
using ProtocolService.Data;
using ProtocolService.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ProtocolDbContext>(opts =>
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("LifeSci360_Services"),
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

// Global exception handler
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var feature = ctx.Features.Get<IExceptionHandlerFeature>();
        var error = feature?.Error;

        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(
            app.Environment.IsDevelopment()
                ? error?.Message ?? "An unexpected error occurred."
                : "An unexpected error occurred. Please try again later.");

        await ctx.Response.WriteAsJsonAsync(response);
    });
});

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