using API_dotnet.Models;

namespace API_dotnet.Services
{
    public interface IRabbitMQPublisher
    {
        Task PublishTelemetryDataAsync(List<FMC650Data> telemetryDataList);
    }
}