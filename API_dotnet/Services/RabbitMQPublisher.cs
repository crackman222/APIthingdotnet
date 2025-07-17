using API_dotnet.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace API_dotnet.Services
{
    public class RabbitMQPublisher : IRabbitMQPublisher
    {
        private readonly IConfiguration _configuration;
        private readonly string _hostname;
        private readonly string _queueName;

        public RabbitMQPublisher(IConfiguration configuration)
        {
            _configuration = configuration;
            _hostname = _configuration["RabbitMQ:Hostname"] ?? "localhost";
            _queueName = _configuration["RabbitMQ:QueueName"] ?? "telemetry_queue";
        }

        public async Task PublishTelemetryDataAsync(List<FMC650Data> telemetryDataList)
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = _hostname };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: _queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                foreach (var data in telemetryDataList)
                {
                    var message = JsonSerializer.Serialize(data, options);
                    var body = Encoding.UTF8.GetBytes(message);
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;

                    channel.BasicPublish(exchange: "",
                                         routingKey: _queueName,
                                         basicProperties: properties,
                                         body: body);
                    Console.WriteLine($" [x] Sent '{message}' to RabbitMQ queue '{_queueName}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing to RabbitMQ: {ex.Message}");
            }
            await Task.CompletedTask;
        }
    }
}