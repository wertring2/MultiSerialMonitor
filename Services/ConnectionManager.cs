using System.Diagnostics;
using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Services
{
    public class ConnectionManager : IDisposable
    {
        private readonly IPortMonitor _monitor;
        private readonly PortConnection _connection;
        private System.Threading.Timer? _reconnectTimer;
        private bool _isDisposed;
        
        public ConnectionManager(IPortMonitor monitor, PortConnection connection)
        {
            _monitor = monitor;
            _connection = connection;
            
            _connection.StatusChanged += OnStatusChanged;
        }
        
        private void OnStatusChanged(object? sender, ConnectionStatus status)
        {
            if (_connection.Config.AutoReconnect && status == ConnectionStatus.Error)
            {
                StartReconnectTimer();
            }
            else if (status == ConnectionStatus.Connected)
            {
                StopReconnectTimer();
            }
        }
        
        private void StartReconnectTimer()
        {
            if (_reconnectTimer != null || _isDisposed) return;
            
            _connection.OnDataReceived($"Auto-reconnect enabled. Will retry in {_connection.Config.ReconnectIntervalMs}ms");
            
            _reconnectTimer = new System.Threading.Timer(async _ =>
            {
                if (_isDisposed || _monitor.IsConnected) return;
                
                try
                {
                    _connection.OnDataReceived("Auto-reconnecting...");
                    await _monitor.ConnectAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Auto-reconnect failed: {ex.Message}");
                    // Timer will continue running for next attempt
                }
            }, null, _connection.Config.ReconnectIntervalMs, _connection.Config.ReconnectIntervalMs);
        }
        
        private void StopReconnectTimer()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
        }
        
        public void Dispose()
        {
            _isDisposed = true;
            StopReconnectTimer();
            _connection.StatusChanged -= OnStatusChanged;
        }
    }
}