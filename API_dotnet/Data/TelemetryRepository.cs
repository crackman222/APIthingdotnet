// Data/TelemetryRepository.cs
using API_dotnet.Models;
using Npgsql;
using System.Text.Json; // Import ini untuk JsonSerializerOptions
using Microsoft.Extensions.Configuration; // Untuk mengakses connection string

namespace API_dotnet.Data
{
    public class TelemetryRepository : ITelemetryRepository
    {
        private readonly string _connectionString;

        public TelemetryRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task SaveTelemetryBatchAsync(List<FMC650Data> telemetryList)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var fmc650Data in telemetryList)
            {
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO data (device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status)
                    VALUES (@device_id, @timestamp, @latitude, @longitude, @speed, @heading, @fuel, @engine_hours, @ignition_status)", conn);

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
        }

        public async Task<List<FMC650Data>> GetAllTelemetryAsync()
        {
            var dataList = new List<FMC650Data>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
                SELECT id, device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status
                FROM data
                ORDER BY timestamp DESC", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var data = new FMC650Data
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    DeviceId = reader["device_id"]?.ToString() ?? "unknown",
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

        public async Task<FMC650Data?> GetTelemetryByIdAsync(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
                SELECT id, device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status
                FROM data
                WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new FMC650Data
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    DeviceId = reader["device_id"]?.ToString() ?? "unknown",
                    Timestamp = (DateTime)reader["timestamp"],
                    Latitude = Convert.ToDouble(reader["latitude"]),
                    Longitude = Convert.ToDouble(reader["longitude"]),
                    Speed = Convert.ToInt32(reader["speed"]),
                    Heading = Convert.ToInt32(reader["heading"]),
                    Fuel = Convert.ToDouble(reader["fuel"]),
                    EngineHours = Convert.ToInt32(reader["engine_hours"]),
                    IgnitionStatus = Convert.ToBoolean(reader["ignition_status"])
                };
            }
            return null;
        }

        public async Task UpdateTelemetryAsync(int id, FMC650Data fmc650Data)
        {
            using var conn = new NpgsqlConnection(_connectionString);
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

        public async Task DeleteTelemetryAsync(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand("DELETE FROM data WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<FMC650Data>> GetLastKnownLocationsAsync()
        {
            var dataList = new List<FMC650Data>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
                SELECT DISTINCT ON (device_id) 
                    id, device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status 
                FROM data 
                ORDER BY device_id, timestamp DESC", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var data = new FMC650Data
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    DeviceId = reader["device_id"]?.ToString() ?? "unknown",
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

        public async Task<List<FMC650Data>> GetDevicePathHistoryAsync(string deviceId, DateTime startDate, DateTime endDate)
        {
            var dataList = new List<FMC650Data>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
                SELECT id, device_id, timestamp, latitude, longitude, speed, heading, fuel, engine_hours, ignition_status
                FROM data
                WHERE device_id = @deviceId AND timestamp >= @startDate AND timestamp <= @endDate
                ORDER BY timestamp ASC", conn);

            cmd.Parameters.AddWithValue("deviceId", deviceId);
            cmd.Parameters.AddWithValue("startDate", startDate);
            cmd.Parameters.AddWithValue("endDate", endDate);

            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var data = new FMC650Data
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    DeviceId = reader["device_id"]?.ToString() ?? "unknown",
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

        public async Task<List<string>> GetUniqueDeviceIdsAsync()
        {
            var deviceIds = new List<string>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand("SELECT DISTINCT device_id FROM data ORDER BY device_id", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                deviceIds.Add(reader["device_id"]?.ToString() ?? "unknown");
            }

            return deviceIds;
        }

        public async Task<DeviceSummary?> GetDeviceSummaryAsync(string deviceId, DateTime startDate, DateTime endDate)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    COUNT(id) AS TotalRecords,
                    AVG(speed) AS AverageSpeed
                FROM data
                WHERE device_id = @deviceId AND timestamp >= @startDate AND timestamp <= @endDate", conn);

            cmd.Parameters.AddWithValue("deviceId", deviceId);
            cmd.Parameters.AddWithValue("startDate", startDate);
            cmd.Parameters.AddWithValue("endDate", endDate);

            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var totalRecords = reader.IsDBNull(reader.GetOrdinal("TotalRecords")) ? 0 : Convert.ToInt32(reader["TotalRecords"]);
                var averageSpeed = reader.IsDBNull(reader.GetOrdinal("AverageSpeed")) ? 0.0 : Convert.ToDouble(reader["AverageSpeed"]);

                return new DeviceSummary
                {
                    DeviceId = deviceId,
                    TotalRecords = totalRecords,
                    AverageSpeed = averageSpeed
                };
            }

            return null;
        }
    }
}