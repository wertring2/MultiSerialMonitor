using System.IO.Ports;
using System.Text;
using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Services
{
    public class SerialPortMonitor : IPortMonitor
    {
        private SerialPort? _serialPort;
        private readonly StringBuilder _lineBuilder = new StringBuilder();
        
        public PortConnection Connection { get; }
        public bool IsConnected => _serialPort?.IsOpen ?? false;
        
        public SerialPortMonitor(PortConnection connection)
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
            var connectTask = Task.Run(() =>
            {
                Connection.SetStatus(ConnectionStatus.Connecting);
                
                // Check if port exists
                var availablePorts = SerialPort.GetPortNames();
                if (!availablePorts.Contains(Connection.PortName))
                {
                    throw new Exception($"Port {Connection.PortName} not found. Available ports: {string.Join(", ", availablePorts)}");
                }
                
                _serialPort = new SerialPort
                {
                    PortName = Connection.PortName,
                    BaudRate = Connection.BaudRate,
                    Parity = Connection.Parity,
                    DataBits = Connection.DataBits,
                    StopBits = Connection.StopBits,
                    ReadTimeout = Connection.Config.ConnectionTimeoutMs,
                    WriteTimeout = Connection.Config.ConnectionTimeoutMs,
                    Handshake = Handshake.None,
                    DtrEnable = true,
                    RtsEnable = true
                };
                
                _serialPort.DataReceived += OnSerialDataReceived;
                _serialPort.ErrorReceived += OnSerialErrorReceived;
                
                _serialPort.Open();
                
                // Verify port is really open
                if (!_serialPort.IsOpen)
                {
                    throw new Exception("Port opened but IsOpen returned false");
                }
                
                Connection.SetStatus(ConnectionStatus.Connected);
                Connection.SetError("");
                Connection.OnDataReceived($"Connected to {Connection.PortName} at {Connection.BaudRate} baud");
            });
            
            if (await Task.WhenAny(connectTask, Task.Delay(Connection.Config.ConnectionTimeoutMs)) != connectTask)
            {
                // Timeout
                _serialPort?.Dispose();
                _serialPort = null;
                throw new TimeoutException($"Connection timeout after {Connection.Config.ConnectionTimeoutMs}ms");
            }
            
            await connectTask; // Propagate any exception
        }
        
        private string GetDetailedErrorMessage(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException uae => GetUnauthorizedAccessMessage(uae),
                IOException ioe when ioe.Message.Contains("not exist") => $"Port {Connection.PortName} does not exist.",
                IOException ioe when ioe.Message.Contains("denied") => $"Access to {Connection.PortName} was denied. Check permissions.",
                TimeoutException => $"Connection timed out. Device may not be responding.",
                InvalidOperationException => $"Invalid port configuration. Check settings.",
                _ => $"Connection failed: {ex.Message}"
            };
        }
        
        private string GetUnauthorizedAccessMessage(UnauthorizedAccessException ex)
        {
            var message = $"Cannot access {Connection.PortName} - port is already in use.";
            
            // Try to detect which process is using the port
            var detectedProcess = PortProcessDetector.GetProcessUsingPort(Connection.PortName);
            if (!string.IsNullOrEmpty(detectedProcess))
            {
                message += $"\n\nDetected application using this port: {detectedProcess}";
                message += $"\n\nPlease close {detectedProcess} and try again.";
            }
            else
            {
                var possibleApps = new List<string>();
                
                if (ex.Message.Contains("COM"))
                {
                    possibleApps.Add("PuTTY");
                    possibleApps.Add("Arduino IDE");
                    possibleApps.Add("Another MultiSerialMonitor instance");
                    possibleApps.Add("Terminal emulator");
                }
                
                if (possibleApps.Any())
                {
                    message += $"\n\nCommon applications that use serial ports:\n• {string.Join("\n• ", possibleApps)}";
                    message += "\n\nPlease close the application using this port and try again.";
                }
            }
            
            return message;
        }
        
        public async Task DisconnectAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (_serialPort?.IsOpen == true)
                    {
                        _serialPort.Close();
                    }
                    Connection.SetStatus(ConnectionStatus.Disconnected);
                }
                catch (Exception ex)
                {
                    Connection.OnDataReceived($"Error disconnecting: {ex.Message}");
                }
                finally
                {
                    _serialPort?.Dispose();
                    _serialPort = null;
                }
            });
        }
        
        public async Task SendCommandAsync(string command)
        {
            if (_serialPort?.IsOpen != true)
            {
                throw new InvalidOperationException("Serial port is not connected");
            }
            
            await Task.Run(() =>
            {
                try
                {
                    _serialPort.WriteLine(command);
                    Connection.OnDataReceived($"> {command}");
                }
                catch (Exception ex)
                {
                    Connection.OnDataReceived($"Error sending command: {ex.Message}");
                    throw;
                }
            });
        }
        
        private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort == null) return;
                
                string data = _serialPort.ReadExisting();
                foreach (char c in data)
                {
                    if (c == '\n')
                    {
                        string line = _lineBuilder.ToString().TrimEnd('\r');
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            Connection.OnDataReceived(line);
                        }
                        _lineBuilder.Clear();
                    }
                    else if (c != '\r') // Skip carriage returns
                    {
                        _lineBuilder.Append(c);
                    }
                }
            }
            catch (Exception ex)
            {
                Connection.OnDataReceived($"Error reading data: {ex.Message}");
            }
        }
        
        private void OnSerialErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Connection.OnDataReceived($"Serial port error: {e.EventType}");
            Connection.SetStatus(ConnectionStatus.Error);
        }
        
        public void Dispose()
        {
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.DataReceived -= OnSerialDataReceived;
                    _serialPort.ErrorReceived -= OnSerialErrorReceived;
                    _serialPort.Close();
                }
                _serialPort?.Dispose();
                _serialPort = null;
            }
            catch
            {
                // Ignore errors during disposal
            }
        }
    }
}