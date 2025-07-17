// Program.cs
using API_dotnet.Data;
using API_dotnet.Services;
using API_dotnet.Endpoints; // Import namespace Endpoints
using System.Text.Json;
using System.Text.Json.Serialization; // Untuk [JsonConverter] jika dibutuhkan
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Baca connection string dari appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Register Repository and Service for Dependency Injection
builder.Services.AddSingleton<ITelemetryRepository>(new TelemetryRepository(builder.Configuration)); // Menggunakan Singleton karena Repository tidak menyimpan state per request
builder.Services.AddScoped<ITelemetryService, TelemetryService>(); // Menggunakan Scoped karena Service mungkin punya logika per request

// Tambahkan Swagger/OpenAPI (sangat direkomendasikan untuk pengembangan API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map all telemetry API endpoints using the extension method
app.MapTelemetryApiEndpoints();

app.Run();