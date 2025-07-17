// Services/TelemetryService.cs
using API_dotnet.Data;
using API_dotnet.Models;

namespace API_dotnet.Services
{
    public class TelemetryService : ITelemetryService
    {
        private readonly ITelemetryRepository _repository;

        public TelemetryService(ITelemetryRepository repository)
        {
            _repository = repository;
        }

        // Anda bisa menambahkan logika bisnis tambahan di sini sebelum memanggil repository
        public async Task SaveTelemetryBatchAsync(List<FMC650Data> telemetryList)
        {
            // Contoh logika: validasi data sebelum disimpan
            if (telemetryList == null || !telemetryList.Any())
            {
                throw new ArgumentException("Telemetry list cannot be null or empty.");
            }
            // Filter data yang mungkin tidak valid atau duplikat (logika bisnis)
            // telemetryList = telemetryList.Where(d => d.Latitude != 0 && d.Longitude != 0).ToList();

            await _repository.SaveTelemetryBatchAsync(telemetryList);
        }

        public async Task<List<FMC650Data>> GetAllTelemetryAsync()
        {
            return await _repository.GetAllTelemetryAsync();
        }

        public async Task<FMC650Data?> GetTelemetryByIdAsync(int id)
        {
            return await _repository.GetTelemetryByIdAsync(id);
        }

        public async Task UpdateTelemetryAsync(int id, FMC650Data fmc650Data)
        {
            await _repository.UpdateTelemetryAsync(id, fmc650Data);
        }

        public async Task DeleteTelemetryAsync(int id)
        {
            await _repository.DeleteTelemetryAsync(id);
        }

        public async Task<List<FMC650Data>> GetLastKnownLocationsAsync()
        {
            return await _repository.GetLastKnownLocationsAsync();
        }

        public async Task<List<FMC650Data>> GetDevicePathHistoryAsync(string deviceId, DateTime startDate, DateTime endDate)
        {
            // Contoh logika: pastikan rentang tanggal masuk akal
            if (startDate > endDate)
            {
                throw new ArgumentException("Start date cannot be after end date.");
            }
            return await _repository.GetDevicePathHistoryAsync(deviceId, startDate, endDate);
        }

        public async Task<List<string>> GetUniqueDeviceIdsAsync()
        {
            return await _repository.GetUniqueDeviceIdsAsync();
        }

        public async Task<DeviceSummary?> GetDeviceSummaryAsync(string deviceId, DateTime startDate, DateTime endDate)
        {
            return await _repository.GetDeviceSummaryAsync(deviceId, startDate, endDate);
        }
    }
}