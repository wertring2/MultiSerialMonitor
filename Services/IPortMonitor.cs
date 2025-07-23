using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Services
{
    public interface IPortMonitor : IDisposable
    {
        PortConnection Connection { get; }
        bool IsConnected { get; }
        
        Task ConnectAsync();
        Task DisconnectAsync();
        Task SendCommandAsync(string command);
    }
}