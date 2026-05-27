using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SampleService.Data;
using SampleService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();//registers all controllers
builder.Services.AddOpenApi();//enables api documentation

// EF Core — connects to LifeSci360_Services database
builder.Services.AddDbContext<ServicesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ServicesDb")));

// JWT Authentication — same secret as AuthService
string jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

// Register repositories
builder.Services.AddScoped<ISampleRepository, SampleRepository>();
builder.Services.AddScoped<ILabResultRepository, LabResultRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
