using Microsoft.EntityFrameworkCore;
using ReportingService.Data;
using ReportingService.Hubs;
using ReportingService.Interfaces;
using ReportingService.Repositories;
using ReportingService.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ServicesDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.UseCompatibilityLevel(160)));

// ── Business Logic & Services ─────────────────────────────────────────────────
builder.Services.AddScoped<IKpiCalculationService, KpiCalculationService>();
builder.Services.AddScoped<IReportValidationService, ReportValidationService>();
builder.Services.AddScoped<IKpiReportRepository, KpiReportRepository>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(opt =>
{
    opt.EnableDetailedErrors = builder.Environment.IsDevelopment();
    opt.KeepAliveInterval = TimeSpan.FromSeconds(15);
    opt.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// ── Background broadcast every 30s ───────────────────────────────────────────
builder.Services.AddHostedService<DashboardBroadcastService>();

// ── Auth ──────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(opt =>
    {
        opt.Authority = builder.Configuration["Auth:Authority"];
        opt.Audience = builder.Configuration["Auth:Audience"];
        opt.RequireHttpsMetadata = false;
        opt.Events = new()
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs/reporting"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(o =>
    o.AddPolicy("AllowFrontend", p =>
        p.WithOrigins(
            builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:4200")
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials()));

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ReportingHub>("/hubs/reporting");

app.Run();