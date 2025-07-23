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
            Connection.ConnectionAttempts = 0;
            Exception? lastException = null;
            
            for (int attempt = 1; attempt <= Connection.Config.MaxRetryAttempts; attempt++)
            {
                Connection.ConnectionAttempts = attempt;
                
                try
                {
                    await ConnectWithTimeoutAsync();
                    return; // Success
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    string errorMsg = GetDetailedErrorMessage(ex);
                    Connection.SetError(errorMsg);
                    Connection.OnDataReceived($"[Attempt {attempt}/{Connection.Config.MaxRetryAttempts}] {errorMsg}");
                    
                    if (attempt < Connection.Config.MaxRetryAttempts)
                    {
                        Connection.OnDataReceived($"Retrying in {Connection.Config.RetryDelayMs}ms...");
                        await Task.Delay(Connection.Config.RetryDelayMs);
                    }
                }
            }
            
            // All attempts failed
            Connection.SetStatus(ConnectionStatus.Error);
            throw new Exception($"Failed to connect after {Connection.Config.MaxRetryAttempts} attempts", lastException);
        }
        
        private async Task ConnectWithTimeoutAsync()
        {
            Connection.SetStatus(ConnectionStatus.Connecting);
            
            var cts = new CancellationTokenSource(Connection.Config.ConnectionTimeoutMs);
            
            try
            {
                _telnetClient = new Client(Connection.HostName, Connection.Port, cts.Token);
                
                // Wait a bit to ensure connection is established
                await Task.Delay(100);
                
                // Test connection
                if (!_telnetClient.IsConnected)
                {
                    throw new Exception("Client created but not connected");
                }
                
                Connection.SetStatus(ConnectionStatus.Connected);
                Connection.SetError("");
                Connection.OnDataReceived($"Connected to {Connection.HostName}:{Connection.Port}");
                
                // Start reading data
                _cancellationTokenSource = new CancellationTokenSource();
                _readTask = Task.Run(() => ReadDataAsync(_cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Connection timeout after {Connection.Config.ConnectionTimeoutMs}ms");
            }
        }
        
        private string GetDetailedErrorMessage(Exception ex)
        {
            return ex switch
            {
                System.Net.Sockets.SocketException se when se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused 
                    => $"Connection refused by {Connection.HostName}:{Connection.Port}. Service may not be running.",
                System.Net.Sockets.SocketException se when se.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound 
                    => $"Host '{Connection.HostName}' not found. Check hostname/IP.",
                System.Net.Sockets.SocketException se when se.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut 
                    => $"Connection timed out. Host may be unreachable.",
                System.Net.Sockets.SocketException se when se.SocketErrorCode == System.Net.Sockets.SocketError.NetworkUnreachable 
                    => $"Network unreachable. Check network connection.",
                TimeoutException => $"Connection timed out after {Connection.Config.ConnectionTimeoutMs}ms.",
                ArgumentException => $"Invalid hostname or port: {Connection.HostName}:{Connection.Port}",
                _ => $"Connection failed: {ex.Message}"
            };
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
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    Connection.OnDataReceived(line);
                                }
                                lineBuilder.Clear();
                            }
                            else if (c != '\0' && c != '\r') // Ignore null characters and carriage returns
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
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _telnetClient?.Dispose();
                _telnetClient = null;
            }
            catch
            {
                // Ignore errors during disposal
            }
        }
    }
}