namespace Hotshield.Models
{
    public class AppRule
    {
        public int Id { get; set; }
        public string AppName { get; set; } = "";
        public string ExePath { get; set; } = "";
        public string? ServiceName { get; set; }
        public string Action { get; set; } = "block";
        public string Direction { get; set; } = "outbound";
        public string RuleGroup { get; set; } = "Custom";
        public bool IsActive { get; set; } = true;
        public string? FirewallRuleName { get; set; }
        public string Notes { get; set; } = "";
    }
}
