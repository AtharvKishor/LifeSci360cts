using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load ocelot.json on top of appsettings.json
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// ── Auth — Gateway validates JWT before forwarding ────────────────────────────
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", opt =>
    {
        opt.Authority = builder.Configuration["Auth:Authority"];
        opt.Audience = builder.Configuration["Auth:Audience"];
        opt.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

// ── Ocelot ────────────────────────────────────────────────────────────────────
builder.Services.AddOcelot(builder.Configuration);

// ── CORS — Angular talks to Gateway, not individual services ──────────────────
builder.Services.AddCors(o =>
    o.AddPolicy("AllowFrontend", p =>
        p.WithOrigins(
            builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:4200")
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials()));

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:53719", "http://127.0.0.1:53719")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors("AllowAngular");

await app.UseOcelot();

app.Run();