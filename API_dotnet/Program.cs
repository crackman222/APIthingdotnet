using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Npgsql;
using System.Text.Json;
using TryConsoleApp.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=ideapad;Database=postgres";

// POST endpoint to save telemetry data
app.MapPost("/telemetry", async (HttpContext context) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var rawJson = await reader.ReadToEndAsync();
        
        Console.WriteLine("========== RAW JSON RECEIVED ==========");
        Console.WriteLine(rawJson);
        Console.WriteLine("========================================");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var fmc650Data = JsonSerializer.Deserialize<FMC650Data>(rawJson, options);

        if (fmc650Data == null)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid payload");
            return;
        }

        await SaveToDatabase(fmc650Data);

        Console.WriteLine($"Parsed Device ID: {fmc650Data.DeviceId}");
        Console.WriteLine($"Timestamp: {fmc650Data.Timestamp}");
        Console.WriteLine($"Lat: {fmc650Data.Latitude}, Lon: {fmc650Data.Longitude}");
        Console.WriteLine($"Speed: {fmc650Data.Speed}, Heading: {fmc650Data.Heading}");
        Console.WriteLine($"Fuel: {fmc650Data.Fuel}, EngineHours: {fmc650Data.EngineHours}, Ignition: {fmc650Data.IgnitionStatus}");
        
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("Data saved successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

// GET endpoint to retrieve all telemetry data
app.MapGet("/telemetry", async (HttpContext context) =>
{
    try
    {
        var data = await GetAllTelemetryData();
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        var jsonResponse = JsonSerializer.Serialize(data, options);
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(jsonResponse);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error retrieving data: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

// GET endpoint to retrieve telemetry data by device ID
app.MapGet("/telemetry/device/{deviceId}", async (HttpContext context) =>
{
    try
    {
        var deviceId = context.Request.RouteValues["deviceId"]?.ToString();
        if (string.IsNullOrEmpty(deviceId))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Device ID is required");
            return;
        }

        var data = await GetTelemetryDataByDevice(deviceId);
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        var jsonResponse = JsonSerializer.Serialize(data, options);
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(jsonResponse);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error retrieving data: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

// GET endpoint to retrieve latest telemetry data (last 10 records)
app.MapGet("/telemetry/latest", async (HttpContext context) =>
{
    try
    {
        var limit = 10;
        if (context.Request.Query.ContainsKey("limit") && 
            int.TryParse(context.Request.Query["limit"], out var parsedLimit))
        {
            limit = parsedLimit;
        }

        var data = await GetLatestTelemetryData(limit);
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        var jsonResponse = JsonSerializer.Serialize(data, options);
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(jsonResponse);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error retrieving latest data: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

// GET endpoint to retrieve telemetry data within a date range
app.MapGet("/telemetry/range", async (HttpContext context) =>
{
    try
    {
        var startDateStr = context.Request.Query["startDate"].ToString();
        var endDateStr = context.Request.Query["endDate"].ToString();
        
        if (string.IsNullOrEmpty(startDateStr) || string.IsNullOrEmpty(endDateStr))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Both startDate and endDate parameters are required (format: yyyy-MM-dd)");
            return;
        }

        if (!DateTime.TryParse(startDateStr, out var startDate) || 
            !DateTime.TryParse(endDateStr, out var endDate))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid date format. Use yyyy-MM-dd format");
            return;
        }

        var data = await GetTelemetryDataByDateRange(startDate, endDate);
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        var jsonResponse = JsonSerializer.Serialize(data, options);
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(jsonResponse);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error retrieving data by date range: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

// GET endpoint to retrieve telemetry data within a specific time range
app.MapGet("/telemetry/timerange", async (HttpContext context) =>
{
    try
    {
        var startTimeStr = context.Request.Query["startTime"].ToString();
        var endTimeStr = context.Request.Query["endTime"].ToString();
        
        if (string.IsNullOrEmpty(startTimeStr) || string.IsNullOrEmpty(endTimeStr))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Both startTime and endTime parameters are required (format: yyyy-MM-ddTHH:mm:ss or yyyy-MM-dd HH:mm:ss)");
            return;
        }

        if (!DateTime.TryParse(startTimeStr, out var startTime) || 
            !DateTime.TryParse(endTimeStr, out var endTime))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid datetime format. Use yyyy-MM-ddTHH:mm:ss or yyyy-MM-dd HH:mm:ss format");
            return;
        }

        if (startTime > endTime)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Start time cannot be greater than end time");
            return;
        }

        var data = await GetTelemetryDataByTimeRange(startTime, endTime);
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        var jsonResponse = JsonSerializer.Serialize(data, options);
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(jsonResponse);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error retrieving data by time range: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

app.Run("http://192.168.17.211:5000");

// Save function 
static async Task SaveToDatabase(FMC650Data fmc650Data)
{
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO data 
        (device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status) 
        VALUES 
        (@device_id, @timestamp, @latitude, @longitude, @speed, @heading, @fuel, @engine_hours, @ignition_status)", conn);

    cmd.Parameters.AddWithValue("device_id", fmc650Data.DeviceId ?? "unknown");
    cmd.Parameters.AddWithValue("timestamp", fmc650Data.Timestamp);
    cmd.Parameters.AddWithValue("latitude", fmc650Data.Latitude);
    cmd.Parameters.AddWithValue("longitude", fmc650Data.Longitude);
    cmd.Parameters.AddWithValue("speed", fmc650Data.Speed);
    cmd.Parameters.AddWithValue("heading", fmc650Data.Heading);
    cmd.Parameters.AddWithValue("fuel", fmc650Data.Fuel);
    cmd.Parameters.AddWithValue("engine_hours", fmc650Data.EngineHours);
    cmd.Parameters.AddWithValue("ignition_status", fmc650Data.IgnitionStatus);

    await cmd.ExecuteNonQueryAsync();
}

// Get all telemetry data
static async Task<List<FMC650Data>> GetAllTelemetryData()
{
    var dataList = new List<FMC650Data>();

    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand(@"
        SELECT device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status 
        FROM data 
        ORDER BY timestamp DESC", conn);

    using var reader = await cmd.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        var data = new FMC650Data
        {
            DeviceId = reader["device_id"].ToString(),
            Timestamp = (DateTime)reader["timestamp"],
            Latitude = Convert.ToDouble(reader["latitude"]),
            Longitude = Convert.ToDouble(reader["longitude"]),
            Speed = Convert.ToInt32(reader["speed"]),
            Heading = Convert.ToInt32(reader["heading"]),
            Fuel = Convert.ToDouble(reader["fuel"]),
            EngineHours = Convert.ToInt32(reader["engine_hours"]),
            IgnitionStatus = Convert.ToBoolean(reader["ignition_status"])
        };
        dataList.Add(data);
    }

    return dataList;
}

// Get telemetry data by device ID
static async Task<List<FMC650Data>> GetTelemetryDataByDevice(string deviceId)
{
    var dataList = new List<FMC650Data>();

    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand(@"
        SELECT device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status 
        FROM data 
        WHERE device_id = @device_id
        ORDER BY timestamp DESC", conn);

    cmd.Parameters.AddWithValue("device_id", deviceId);

    using var reader = await cmd.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        var data = new FMC650Data
        {
            DeviceId = reader["device_id"].ToString(),
            Timestamp = (DateTime)reader["timestamp"],
            Latitude = Convert.ToDouble(reader["latitude"]),
            Longitude = Convert.ToDouble(reader["longitude"]),
            Speed = Convert.ToInt32(reader["speed"]),
            Heading = Convert.ToInt32(reader["heading"]),
            Fuel = Convert.ToDouble(reader["fuel"]),
            EngineHours = Convert.ToInt32(reader["engine_hours"]),
            IgnitionStatus = Convert.ToBoolean(reader["ignition_status"])
        };
        dataList.Add(data);
    }

    return dataList;
}

// Get latest telemetry data (limited number of records)
static async Task<List<FMC650Data>> GetLatestTelemetryData(int limit)
{
    var dataList = new List<FMC650Data>();

    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand(@"
        SELECT device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status 
        FROM data 
        ORDER BY timestamp DESC 
        LIMIT @limit", conn);

    cmd.Parameters.AddWithValue("limit", limit);

    using var reader = await cmd.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        var data = new FMC650Data
        {
            DeviceId = reader["device_id"].ToString(),
            Timestamp = (DateTime)reader["timestamp"],
            Latitude = Convert.ToDouble(reader["latitude"]),
            Longitude = Convert.ToDouble(reader["longitude"]),
            Speed = Convert.ToInt32(reader["speed"]),
            Heading = Convert.ToInt32(reader["heading"]),
            Fuel = Convert.ToDouble(reader["fuel"]),
            EngineHours = Convert.ToInt32(reader["engine_hours"]),
            IgnitionStatus = Convert.ToBoolean(reader["ignition_status"])
        };
        dataList.Add(data);
    }

    return dataList;
}

// Get telemetry data by date range
static async Task<List<FMC650Data>> GetTelemetryDataByDateRange(DateTime startDate, DateTime endDate)
{
    var dataList = new List<FMC650Data>();

    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand(@"
        SELECT device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status 
        FROM data 
        WHERE timestamp >= @start_date AND timestamp <= @end_date
        ORDER BY timestamp DESC", conn);

    cmd.Parameters.AddWithValue("start_date", startDate);
    cmd.Parameters.AddWithValue("end_date", endDate.AddDays(1)); // Include the entire end date

    using var reader = await cmd.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        var data = new FMC650Data
        {
            DeviceId = reader["device_id"].ToString(),
            Timestamp = (DateTime)reader["timestamp"],
            Latitude = Convert.ToDouble(reader["latitude"]),
            Longitude = Convert.ToDouble(reader["longitude"]),
            Speed = Convert.ToInt32(reader["speed"]),
            Heading = Convert.ToInt32(reader["heading"]),
            Fuel = Convert.ToDouble(reader["fuel"]),
            EngineHours = Convert.ToInt32(reader["engine_hours"]),
            IgnitionStatus = Convert.ToBoolean(reader["ignition_status"])
        };
        dataList.Add(data);
    }

    return dataList;
}

// Get telemetry data by time range (with specific time)
static async Task<List<FMC650Data>> GetTelemetryDataByTimeRange(DateTime startTime, DateTime endTime)
{
    var dataList = new List<FMC650Data>();

    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand(@"
        SELECT device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status 
        FROM data 
        WHERE timestamp >= @start_time AND timestamp <= @end_time
        ORDER BY timestamp DESC", conn);

    cmd.Parameters.AddWithValue("start_time", startTime);
    cmd.Parameters.AddWithValue("end_time", endTime);

    using var reader = await cmd.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        var data = new FMC650Data
        {
            DeviceId = reader["device_id"].ToString(),
            Timestamp = (DateTime)reader["timestamp"],
            Latitude = Convert.ToDouble(reader["latitude"]),
            Longitude = Convert.ToDouble(reader["longitude"]),
            Speed = Convert.ToInt32(reader["speed"]),
            Heading = Convert.ToInt32(reader["heading"]),
            Fuel = Convert.ToDouble(reader["fuel"]),
            EngineHours = Convert.ToInt32(reader["engine_hours"]),
            IgnitionStatus = Convert.ToBoolean(reader["ignition_status"])
        };
        dataList.Add(data);
    }

    return dataList;
}