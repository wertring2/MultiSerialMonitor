using System.Text;
using PrimS.Telnet;
using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Services
{
    public class TelnetMonitor : IPortMonitor
    {
        private Client? _telnetClient;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _readTask;
        
        public PortConnection Connection { get; }
        public bool IsConnected => _telnetClient?.IsConnected ?? false;
        
        public TelnetMonitor(PortConnection connection)
        {
            Connection = connection;
        }
        
        public async Task ConnectAsync()
        {
            try
            {
                Connection.SetStatus(ConnectionStatus.Connecting);
                
                _telnetClient = new Client(Connection.HostName, Connection.Port, CancellationToken.None);
                
                // Test connection
                if (_telnetClient.IsConnected)
                {
                    Connection.SetStatus(ConnectionStatus.Connected);
                    
                    // Start reading data
                    _cancellationTokenSource = new CancellationTokenSource();
                    _readTask = Task.Run(() => ReadDataAsync(_cancellationTokenSource.Token));
                }
                else
                {
                    throw new Exception("Failed to connect to Telnet server");
                }
            }
            catch (Exception ex)
            {
                Connection.SetStatus(ConnectionStatus.Error);
                Connection.OnDataReceived($"Error: {ex.Message}");
                throw;
            }
        }
        
        public async Task DisconnectAsync()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                
                if (_readTask != null)
                {
                    try
                    {
                        await _readTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelling
                    }
                }
                
                _telnetClient?.Dispose();
                _telnetClient = null;
                
                Connection.SetStatus(ConnectionStatus.Disconnected);
            }
            catch (Exception ex)
            {
                Connection.OnDataReceived($"Error disconnecting: {ex.Message}");
            }
        }
        
        public async Task SendCommandAsync(string command)
        {
            if (_telnetClient?.IsConnected != true)
            {
                throw new InvalidOperationException("Telnet client is not connected");
            }
            
            try
            {
                await _telnetClient.WriteLineAsync(command);
                Connection.OnDataReceived($"> {command}");
            }
            catch (Exception ex)
            {
                Connection.OnDataReceived($"Error sending command: {ex.Message}");
                throw;
            }
        }
        
        private async Task ReadDataAsync(CancellationToken cancellationToken)
        {
            var lineBuilder = new StringBuilder();
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && _telnetClient?.IsConnected == true)
                {
                    string data = await _telnetClient.ReadAsync();
                    
                    if (!string.IsNullOrEmpty(data))
                    {
                        foreach (char c in data)
                        {
                            if (c == '\n')
                            {
                                string line = lineBuilder.ToString().TrimEnd('\r');
                                if (!string.IsNullOrEmpty(line))
                                {
                                    Connection.OnDataReceived(line);
                                }
                                lineBuilder.Clear();
                            }
                            else if (c != '\0') // Ignore null characters
                            {
                                lineBuilder.Append(c);
                            }
                        }
                    }
                    
                    await Task.Delay(10, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when disconnecting
            }
            catch (Exception ex)
            {
                Connection.OnDataReceived($"Error reading data: {ex.Message}");
                Connection.SetStatus(ConnectionStatus.Error);
            }
        }
        
        public void Dispose()
        {
            DisconnectAsync().Wait();
        }
    }
}