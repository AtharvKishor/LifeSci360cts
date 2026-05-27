using Microsoft.EntityFrameworkCore;
using SampleService.Data;
using SampleService.Extensions;
using SampleService.Repositories;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAngularCors(builder.Configuration);
builder.Services.AddDbContext<ServicesDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ServicesDb")));
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddScoped<ISampleRepository, SampleRepository>();
builder.Services.AddScoped<ILabResultRepository, LabResultRepository>();

builder.Services.AddSharedControllers();
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

app.UseSwaggerInDevelopment();
app.UseSharedMiddleware();
app.MapControllers();
app.Run();
