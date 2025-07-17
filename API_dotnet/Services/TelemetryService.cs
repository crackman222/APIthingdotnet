using API_dotnet.Data;
using API_dotnet.Models;
using API_dotnet.Services;

namespace API_dotnet.Services
{
    public class TelemetryService : ITelemetryService
    {
        private readonly ITelemetryRepository _repository;
        private readonly IRabbitMQPublisher _rabbitmqPublisher;

        public TelemetryService(ITelemetryRepository repository, IRabbitMQPublisher rabbitmqPublisher)
        {
            _repository = repository;
            _rabbitmqPublisher = rabbitmqPublisher;
        }

        public async Task SaveTelemetryBatchAsync(List<FMC650Data> telemetryList)
        {
            if (telemetryList == null || !telemetryList.Any())
            {
                throw new ArgumentException("Telemetry list cannot be null or empty.");
            }
            await _repository.SaveTelemetryBatchAsync(telemetryList); // simpan data ke database
            await _rabbitmqPublisher.PublishTelemetryDataAsync(telemetryList); // kirim data ke RabbitMQ
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