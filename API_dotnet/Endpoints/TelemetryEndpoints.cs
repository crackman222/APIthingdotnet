using API_dotnet.Models;
using API_dotnet.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace API_dotnet.Endpoints
{
    public static class TelemetryEndpoints
    {
        public static void MapTelemetryApiEndpoints(this WebApplication app)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            // POST: Save Batch Telemetry Data from Mobile App
            app.MapPost("/telemetry", async (ITelemetryService service, HttpContext context) =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    var rawJson = await reader.ReadToEndAsync();
                    var telemetryList = JsonSerializer.Deserialize<List<FMC650Data>>(rawJson, jsonOptions);

                    if (telemetryList == null || telemetryList.Count == 0)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid or empty payload.");
                        return;
                    }

                    await service.SaveTelemetryBatchAsync(telemetryList);

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync($"Successfully saved {telemetryList.Count} telemetry records.");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Validation error: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync($"Bad Request: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving telemetry data: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Internal Server Error");
                }
            });

            // GET: All Telemetry Data
            app.MapGet("/telemetry", async (ITelemetryService service, HttpContext context) =>
            {
                try
                {
                    var data = await service.GetAllTelemetryAsync();
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(data, jsonOptions));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving all telemetry data: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Internal Server Error");
                }
            });

            // GET: Telemetry by ID
            app.MapGet("/telemetry/{id:int}", async (ITelemetryService service, HttpContext context, int id) =>
            {
                try
                {
                    var data = await service.GetTelemetryByIdAsync(id);
                    if (data == null)
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        await context.Response.WriteAsync($"Data with ID {id} not found.");
                        return;
                    }
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(data, jsonOptions));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving telemetry by ID: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Internal Server Error");
                }
            });

            // PUT: Update Telemetry by ID
            app.MapPut("/telemetry/{id:int}", async (ITelemetryService service, HttpContext context, int id) =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    var rawJson = await reader.ReadToEndAsync();
                    var fmc650Data = JsonSerializer.Deserialize<FMC650Data>(rawJson, jsonOptions);

                    if (fmc650Data == null)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid payload");
                        return;
                    }
                    await service.UpdateTelemetryAsync(id, fmc650Data);

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync($"Data with ID {id} updated successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating data: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Internal Server Error");
                }
            });

            // DELETE: Delete Telemetry by ID
            app.MapDelete("/telemetry/{id:int}", async (ITelemetryService service, HttpContext context, int id) =>
            {
                try
                {
                    await service.DeleteTelemetryAsync(id);

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync($"Data with ID {id} deleted successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting data: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Internal Server Error");
                }
            });

            // GET: Last Known Location for All Devices
            app.MapGet("/telemetry/locations/latest", async (ITelemetryService service, HttpContext context) =>
            {
                try
                {
                    var data = await service.GetLastKnownLocationsAsync();
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(data, jsonOptions));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving last known locations: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Internal Server Error");
                }
            });

            // GET: Path History for a Specific Device
            app.MapGet("/devices/{deviceId}/path-history", async (ITelemetryService service, HttpContext context, string deviceId, DateTime? startDate, DateTime? endDate) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(deviceId))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Device ID is required.");
                        return;
                    }

                    // Handle default date range if not provided or invalid
                    DateTime actualStartDate = startDate ?? DateTime.Today;
                    DateTime actualEndDate = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
                    actualEndDate = actualEndDate.Date.AddDays(1).AddSeconds(-1); // Adjust to cover the whole day

                    if (actualStartDate > actualEndDate)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Start date cannot be after end date.");
                        return;
                    }

                    var data = await service.GetDevicePathHistoryAsync(deviceId, actualStartDate, actualEndDate);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(data, jsonOptions));
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Validation error: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync($"Bad Request: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving device path history: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Internal Server Error");
                }
            });

            // GET: Unique Device IDs
            app.MapGet("/devices", async (ITelemetryService service, HttpContext context) =>
            {
                try
                {
                    var deviceIds = await service.GetUniqueDeviceIdsAsync();
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(deviceIds, jsonOptions));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving unique device IDs: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Internal Server Error");
                }
            });

            // GET: Device Summary
            app.MapGet("/devices/{deviceId}/summary", async (ITelemetryService service, HttpContext context, string deviceId, DateTime? startDate, DateTime? endDate) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(deviceId))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Device ID is required.");
                        return;
                    }

                    DateTime actualStartDate = startDate ?? DateTime.Today;
                    DateTime actualEndDate = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
                    actualEndDate = actualEndDate.Date.AddDays(1).AddSeconds(-1); // Adjust to cover the whole day

                    var summary = await service.GetDeviceSummaryAsync(deviceId, actualStartDate, actualEndDate);
                    
                    if (summary == null)
                    {
                        summary = new DeviceSummary { DeviceId = deviceId, TotalRecords = 0, AverageSpeed = 0.0 };
                    }

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(summary, jsonOptions));
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Validation error: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync($"Bad Request: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving device summary: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Internal Server Error");
                }
            });
        }
    }
}