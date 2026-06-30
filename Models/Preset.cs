namespace Hotshield.Models
{
    public class Preset
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsBuiltin { get; set; }
    }
}
