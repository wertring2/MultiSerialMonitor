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
        public event EventHandler<string>? DataReceived;
        public event EventHandler<ConnectionStatus>? StatusChanged;
        
        public void OnDataReceived(string data)
        {
            LastLine = data;
            OutputHistory.Add($"[{DateTime.Now:HH:mm:ss}] {data}");
            LastActivity = DateTime.Now;
            DataReceived?.Invoke(this, data);
        }
        
        public void SetStatus(ConnectionStatus status)
        {
            Status = status;
            StatusChanged?.Invoke(this, status);
        }
    }
}