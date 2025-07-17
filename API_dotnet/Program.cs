using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Npgsql;
using System.Text.Json;
using TryConsoleApp.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=ideapad;Database=postgres";

// POST endpoint to save telemetry data
app.MapPost("/telemetry", async (HttpContext context) => {
    try {
        using var reader = new StreamReader(context.Request.Body);
        var rawJson = await reader.ReadToEndAsync();
        
        Console.WriteLine("========== RAW JSON RECEIVED ==========");
        Console.WriteLine(rawJson);
        Console.WriteLine("========================================");

        var options = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };

        var fmc650Data = JsonSerializer.Deserialize<FMC650Data>(rawJson, options);

        if (fmc650Data == null) {
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
    } catch (Exception ex) {
        Console.WriteLine($"Error: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

// GET endpoint to retrieve all telemetry data
app.MapGet("/telemetry", async (HttpContext context) => {
    try {
        var data = await GetAllTelemetryData();
        
        var options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        var jsonResponse = JsonSerializer.Serialize(data, options);
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(jsonResponse);
    } catch (Exception ex) {
        Console.WriteLine($"Error retrieving data: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

// GET endpoint to retrieve telemetry data by device ID
app.MapGet("/telemetry/device/{deviceId}", async (HttpContext context) => {
    try {
        var deviceId = context.Request.RouteValues["deviceId"]?.ToString();
        if (string.IsNullOrEmpty(deviceId)) {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Device ID is required");
            return;
        }

        var data = await GetTelemetryDataByDevice(deviceId);
        
        var options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        var jsonResponse = JsonSerializer.Serialize(data, options);
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(jsonResponse);
    } catch (Exception ex) {
        Console.WriteLine($"Error retrieving data: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

// GET endpoint to retrieve latest telemetry data (last 10 records)
app.MapGet("/telemetry/latest", async (HttpContext context) => {
    try {
        var limit = 10;
        if (context.Request.Query.ContainsKey("limit") && 
            int.TryParse(context.Request.Query["limit"], out var parsedLimit)) {
            limit = parsedLimit;
        }

        var data = await GetLatestTelemetryData(limit);
        
        var options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        var jsonResponse = JsonSerializer.Serialize(data, options);
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(jsonResponse);
    } catch (Exception ex) {
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

// DELETE endpoint to delete telemetry data by ID
app.MapDelete("/telemetry/{id}", async (HttpContext context) =>
{
    try
    {
        var idStr = context.Request.RouteValues["id"]?.ToString();
        if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out var id))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Valid ID is required");
            return;
        }

        await DeleteFromDatabase(id);

        context.Response.StatusCode = 200;
        await context.Response.WriteAsync($"Data with ID {id} deleted successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deleting data: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

// PUT endpoint to update telemetry data by ID
app.MapPut("/telemetry/{id}", async (HttpContext context) =>
{
    try
    {
        var idStr = context.Request.RouteValues["id"]?.ToString();
        if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out var id))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Valid ID is required");
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var rawJson = await reader.ReadToEndAsync();
        
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

        await UpdateDatabase(id, fmc650Data);

        context.Response.StatusCode = 200;
        await context.Response.WriteAsync($"Data with ID {id} updated successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error updating data: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

// GET endpoint to retrieve path history for a specific device
app.MapGet("/devices/{deviceId}/path-history", async (HttpContext context) =>
{
    try
    {
        var deviceId = context.Request.RouteValues["deviceId"]?.ToString();
        
        // Ambil parameter tanggal dari query string (misal: ?startDate=2025-07-16&endDate=2025-07-17)
        var startDateStr = context.Request.Query["startDate"].ToString();
        var endDateStr = context.Request.Query["endDate"].ToString();

        if (string.IsNullOrEmpty(deviceId))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Device ID is required.");
            return;
        }

        DateTime startDate;
        DateTime endDate;

        // Validasi dan parsing tanggal
        if (!DateTime.TryParse(startDateStr, out startDate) || !DateTime.TryParse(endDateStr, out endDate))
        {
            // Jika tanggal tidak disediakan atau tidak valid, gunakan rentang default (misal: hari ini)
            // Sesuaikan dengan kebutuhan Anda. Contoh ini menggunakan hari ini.
            startDate = DateTime.Today;
            endDate = DateTime.Today.AddDays(1).AddSeconds(-1); // Sampai akhir hari ini
            // Atau bisa juga kembalikan error 400 jika tanggal wajib
            // context.Response.StatusCode = 400;
            // await context.Response.WriteAsync("Valid startDate and endDate are required in YYYY-MM-DD format.");
            // return;
        }
        
        // Pastikan endDate mencakup seluruh hari yang diminta
        endDate = endDate.Date.AddDays(1).AddSeconds(-1); // Set ke 23:59:59 pada endDate

        var data = await GetDevicePathHistory(deviceId, startDate, endDate);
        
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
        Console.WriteLine($"Error retrieving device path history: {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

app.Run("http://localhost:5001");

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

// Delete function
static async Task DeleteFromDatabase(int id)
{
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand("DELETE FROM data WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);

    await cmd.ExecuteNonQueryAsync();
}

// Update function
static async Task UpdateDatabase(int id, FMC650Data fmc650Data)
{
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand(@"
        UPDATE data SET
            device_id = @device_id,
            timestamp = @timestamp,
            latitude = @latitude,
            longitude = @longitude,
            speed = @speed,
            heading = @heading,
            fuel = @fuel,
            engine_hours = @engine_hours,
            ignition_status = @ignition_status
        WHERE id = @id", conn);

    cmd.Parameters.AddWithValue("id", id);
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

// Function to get path history for a specific device within a date range
static async Task<List<FMC650Data>> GetDevicePathHistory(string deviceId, DateTime startDate, DateTime endDate)
{
    var dataList = new List<FMC650Data>();

    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand(@"
        SELECT id, device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status
        FROM data
        WHERE device_id = @deviceId AND timestamp >= @startDate AND timestamp <= @endDate
        ORDER BY timestamp ASC", conn); // Urutkan berdasarkan waktu agar jalur berurutan

    cmd.Parameters.AddWithValue("deviceId", deviceId);
    cmd.Parameters.AddWithValue("startDate", startDate);
    cmd.Parameters.AddWithValue("endDate", endDate);

    using var reader = await cmd.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        var data = new FMC650Data
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
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