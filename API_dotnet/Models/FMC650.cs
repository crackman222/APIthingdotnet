namespace TryConsoleApp.Models
{
    public class FMC650Data
    {
        public string DeviceId { get; set; } = "FMC650_001";
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