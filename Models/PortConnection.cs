using System.IO.Ports;

namespace MultiSerialMonitor.Models
{
    public enum ConnectionType
    {
        SerialPort,
        Telnet
    }

    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    public class PortConnection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public ConnectionType Type { get; set; }
        public ConnectionStatus Status { get; set; } = ConnectionStatus.Disconnected;
        public string LastLine { get; set; } = "";
        public List<string> OutputHistory { get; } = new List<string>();
        
        // Serial Port specific properties
        public string PortName { get; set; } = "";
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        
        // Telnet specific properties
        public string HostName { get; set; } = "";
        public int Port { get; set; } = 23;
        
        public DateTime LastActivity { get; set; } = DateTime.Now;
        public string? LastError { get; set; }
        public DateTime? LastErrorTime { get; set; }
        public int ConnectionAttempts { get; set; }
        public ConnectionConfig Config { get; set; } = new ConnectionConfig();
        
        public event EventHandler<string>? DataReceived;
        public event EventHandler<ConnectionStatus>? StatusChanged;
        public event EventHandler<string>? ErrorOccurred;
        
        public void OnDataReceived(string data)
        {
            LastLine = data;
            // Check if data already contains timestamp patterns like [16:34:39] or [*07/01/2025 10:38:59.5433]
            bool hasTimestamp = false;
            if (data.Length > 0 && data[0] == '[')
            {
                int closeBracket = data.IndexOf(']');
                if (closeBracket > 1 && closeBracket < 50) // Reasonable timestamp length
                {
                    hasTimestamp = true;
                }
            }
            
            if (hasTimestamp)
            {
                OutputHistory.Add(data);
            }
            else
            {
                OutputHistory.Add($"[{DateTime.Now:HH:mm:ss}] {data}");
            }
            
            LastActivity = DateTime.Now;
            DataReceived?.Invoke(this, data);
        }
        
        public void SetStatus(ConnectionStatus status)
        {
            Status = status;
            StatusChanged?.Invoke(this, status);
        }
        
        public void SetError(string error)
        {
            LastError = error;
            LastErrorTime = DateTime.Now;
            ErrorOccurred?.Invoke(this, error);
        }
    }
}