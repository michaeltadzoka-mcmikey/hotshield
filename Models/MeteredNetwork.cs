namespace Hotshield.Models
{
    public class MeteredNetwork
    {
        public int Id { get; set; }
        public string Ssid { get; set; } = "";
        public string Label { get; set; } = "";
        public int? PresetId { get; set; }
        public string CreatedAt { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}
