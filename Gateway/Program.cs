using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load ocelot.json
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Allow self-signed dev certificates when Gateway calls PatientService
builder.Services
    .AddHttpClient("ocelot")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

// Add Ocelot
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

await app.UseOcelot();

app.Run();