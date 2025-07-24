namespace MultiSerialMonitor.Models
{
    public class DetectionMatch
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PatternId { get; set; } = "";
        public string PatternName { get; set; } = "";
        public string MatchedText { get; set; } = "";
        public string FullLine { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int LineNumber { get; set; }
    }
}