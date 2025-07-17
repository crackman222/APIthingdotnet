// Program.cs
using API_dotnet.Data;
using API_dotnet.Services;
using API_dotnet.Endpoints;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddSingleton<ITelemetryRepository>(new TelemetryRepository(builder.Configuration));
builder.Services.AddScoped<ITelemetryService, TelemetryService>();

builder.Services.AddSingleton<IRabbitMQPublisher, RabbitMQPublisher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // comment jika tidak menggunakan HTTPS di 5001/8001
app.Urls.Add("http://0.0.0.0:8000"); 

app.MapTelemetryApiEndpoints();
app.Run();