namespace MultiSerialMonitor.Models
{
    public class ConnectionConfig
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public int ConnectionTimeoutMs { get; set; } = 5000;
        public bool AutoReconnect { get; set; } = true;
        public int ReconnectIntervalMs { get; set; } = 5000;
        public List<DetectionPattern> DetectionPatterns { get; set; } = new List<DetectionPattern>();
    }
}