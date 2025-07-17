// Models/FMC650Data.cs
namespace API_dotnet.Models
{
    public class FMC650Data
    {
        public int Id { get; set; } // Pastikan properti ini ada dan public
        public string? DeviceId { get; set; } // '?' untuk nullable string
        public DateTime Timestamp { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Speed { get; set; }
        public int Heading { get; set; }
        public double Fuel { get; set; }
        public int EngineHours { get; set; }
        public bool IgnitionStatus { get; set; }
    }
}