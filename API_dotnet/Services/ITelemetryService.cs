// Services/ITelemetryService.cs
using API_dotnet.Models;

namespace API_dotnet.Services
{
    public interface ITelemetryService
    {
        Task SaveTelemetryBatchAsync(List<FMC650Data> telemetryList);
        Task<List<FMC650Data>> GetAllTelemetryAsync();
        Task<FMC650Data?> GetTelemetryByIdAsync(int id);
        Task UpdateTelemetryAsync(int id, FMC650Data fmc650Data);
        Task DeleteTelemetryAsync(int id);
        Task<List<FMC650Data>> GetLastKnownLocationsAsync();
        Task<List<FMC650Data>> GetDevicePathHistoryAsync(string deviceId, DateTime startDate, DateTime endDate);
        Task<List<string>> GetUniqueDeviceIdsAsync();
        Task<DeviceSummary?> GetDeviceSummaryAsync(string deviceId, DateTime startDate, DateTime endDate);
    }
}