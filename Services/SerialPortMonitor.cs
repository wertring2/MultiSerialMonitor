using System.IO.Ports;
using System.Text;
using MultiSerialMonitor.Models;
using MultiSerialMonitor.Exceptions;

namespace MultiSerialMonitor.Services
{
    public class SerialPortMonitor : IPortMonitor
    {
        private SerialPort? _serialPort;
        private readonly StringBuilder _lineBuilder = new StringBuilder();
        
        public PortConnection Connection { get; }
        public bool IsConnected => _serialPort?.IsOpen ?? false;
        
        public static bool IsPortAvailable(string portName)
        {
            try
            {
                using (var testPort = new SerialPort(portName))
                {
                    testPort.Open();
                    testPort.Close();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        
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
                catch (PortNotFoundException ex)
                {
                    // Re-throw PortNotFoundException immediately without retries
                    Connection.SetStatus(ConnectionStatus.Error);
                    Connection.SetError(ex.Message);
                    throw;
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
                    throw new PortNotFoundException($"Port {Connection.PortName} not found. Available ports: {string.Join(", ", availablePorts)}");
                }
                
                // Skip availability check here as it might give false positives
                // The actual open will catch if port is in use
                
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
                
                try
                {
                    _serialPort.Open();
                }
                catch (UnauthorizedAccessException)
                {
                    throw new Exception($"Port {Connection.PortName} is already in use by another application or connection.");
                }
                catch (IOException ioEx)
                {
                    throw new Exception($"Port {Connection.PortName} IO error: {ioEx.Message}");
                }
                
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
            
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentException("Command cannot be empty");
            }
            
            await Task.Run(() =>
            {
                try
                {
                    // Add timeout for write operation
                    _serialPort.WriteTimeout = 5000; // 5 seconds
                    _serialPort.WriteLine(command);
                    Connection.OnDataReceived($"> {command}");
                }
                catch (TimeoutException)
                {
                    var error = "Timeout sending command - device not responding";
                    Connection.OnDataReceived($"Error: {error}");
                    throw new TimeoutException(error);
                }
                catch (InvalidOperationException ex)
                {
                    var error = "Port was closed unexpectedly";
                    Connection.OnDataReceived($"Error: {error}");
                    Connection.SetStatus(ConnectionStatus.Error);
                    throw new InvalidOperationException(error, ex);
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
            catch (InvalidOperationException ioEx)
            {
                // Port might have been closed or reset
                Connection.OnDataReceived($"[READ ERROR] Port may have been reset: {ioEx.Message}");
                Connection.SetError($"Port read error: {ioEx.Message}");
                Connection.SetStatus(ConnectionStatus.Error);
                
                // Try to recover if auto-reconnect is enabled
                if (Connection.Config.AutoReconnect)
                {
                    Connection.OnDataReceived("[INFO] Will attempt auto-reconnect...");
                }
            }
            catch (Exception ex)
            {
                Connection.OnDataReceived($"[READ ERROR] Unexpected error: {ex.Message}");
                Connection.SetError($"Read error: {ex.Message}");
            }
        }
        
        private void OnSerialErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            string errorDescription = GetErrorDescription(e.EventType);
            Connection.OnDataReceived($"[SERIAL ERROR] {errorDescription}");
            
            // For framing errors during device boot, don't set error status immediately
            if (e.EventType == SerialError.Frame)
            {
                Connection.OnDataReceived("[INFO] Framing error detected - this is common during device boot/reset");
                Connection.OnDataReceived("[INFO] The connection will continue - data may be garbled temporarily");
                
                // Just log the error but don't change connection status
                Connection.SetError($"Temporary framing error during device boot");
                
                // Clear the line builder to avoid corrupted data
                _lineBuilder.Clear();
                
                // Don't set error status - let it continue
                return;
            }
            
            // For other errors, set error status
            Connection.SetError($"Serial port error: {errorDescription}");
            Connection.SetStatus(ConnectionStatus.Error);
            
            // Log additional debug info
            Connection.OnDataReceived($"[DEBUG] Error Type: {e.EventType}");
            Connection.OnDataReceived($"[DEBUG] Port State: IsOpen={_serialPort?.IsOpen}, DSR={_serialPort?.DsrHolding}, CTS={_serialPort?.CtsHolding}");
        }
        
        private string GetErrorDescription(SerialError errorType)
        {
            return errorType switch
            {
                SerialError.Frame => "Framing error detected - check baud rate and data format settings",
                SerialError.Overrun => "Buffer overrun - data loss may have occurred",
                SerialError.RXOver => "Receive buffer overflow - incoming data too fast",
                SerialError.RXParity => "Parity error - check parity settings or cable connection",
                SerialError.TXFull => "Transmit buffer full",
                _ => $"{errorType} error"
            };
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