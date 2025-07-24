namespace MultiSerialMonitor.Models
{
    public class DetectionPattern
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Pattern { get; set; } = "";
        public bool IsRegex { get; set; } = false;
        public bool CaseSensitive { get; set; } = false;
        public bool IsEnabled { get; set; } = true;
        public string? NotificationColor { get; set; } = "#FF0000";
    }
}