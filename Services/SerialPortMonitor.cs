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
                    throw new Exception($"Access Denied - Port {Connection.PortName}\n\n" +
                                      "The port is already in use by another application.\n\n" +
                                      "• Close Arduino IDE, PuTTY, or other serial programs\n" +
                                      "• Check Task Manager for background processes\n" +
                                      "• Try running as Administrator if needed");
                }
                catch (IOException ioEx)
                {
                    string detailedMessage = GetIOErrorMessage(ioEx);
                    throw new Exception(detailedMessage);
                }
                catch (InvalidOperationException iopEx)
                {
                    throw new Exception($"Configuration Error - Port {Connection.PortName}\n\n" +
                                      $"Invalid port settings: {iopEx.Message}\n\n" +
                                      "• Check baud rate, data bits, parity settings\n" +
                                      "• Verify the port configuration");
                }
                catch (ArgumentException argEx)
                {
                    throw new Exception($"Invalid Port - {Connection.PortName}\n\n" +
                                      $"Port name is invalid: {argEx.Message}\n\n" +
                                      "• Check the port name format\n" +
                                      "• Refresh the port list");
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
                throw new TimeoutException($"Connection Timeout - Port {Connection.PortName}\n\n" +
                                         $"Failed to connect within {Connection.Config.ConnectionTimeoutMs / 1000} seconds.\n\n" +
                                         "• Device may not be responding\n" +
                                         "• Check power and connections\n" +
                                         "• Try a longer timeout in connection settings\n" +
                                         "• Verify correct baud rate");
            }
            
            await connectTask; // Propagate any exception
        }
        
        private string GetIOErrorMessage(IOException ioEx)
        {
            string message = ioEx.Message.ToLower();
            
            if (message.Contains("device attached to the system is not functioning") ||
                message.Contains("device is not functioning"))
            {
                return $"Device Error - Port {Connection.PortName}\n\n" +
                       "The connected device is not functioning properly.\n\n" +
                       "Troubleshooting steps:\n" +
                       "• Check device connection\n" +
                       "• Unplug and reconnect USB cable\n" +
                       "• Restart device or computer\n" +
                       "• Check Device Manager for driver issues\n" +
                       "• Try a different USB port\n\n" +
                       "If the problem persists, the device may be faulty.";
            }
            else if (message.Contains("port does not exist") || message.Contains("not exist"))
            {
                return $"Port Not Found - {Connection.PortName}\n\n" +
                       "The device may have been disconnected.\n\n" +
                       "• Check physical connection\n" +
                       "• Refresh port list in Add Port dialog\n" +
                       "• Check Device Manager";
            }
            else if (message.Contains("access is denied") || message.Contains("denied"))
            {
                return $"Access Denied - Port {Connection.PortName}\n\n" +
                       "The port is in use by another application\nor you lack permissions.\n\n" +
                       "• Close other serial programs\n" +
                       "• Run as Administrator if needed";
            }
            else if (message.Contains("sharing violation") || message.Contains("already in use"))
            {
                return $"Port In Use - {Connection.PortName}\n\n" +
                       "Another application is using this port.\n\n" +
                       "• Close Arduino IDE, PuTTY, or other serial tools\n" +
                       "• Check Task Manager for background processes";
            }
            else
            {
                return $"Connection Error - Port {Connection.PortName}\n\n" +
                       $"{ioEx.Message}\n\n" +
                       "• Try disconnecting and reconnecting the device\n" +
                       "• Check cables and connections";
            }
        }
        
        private bool IsDeviceDisconnectedError(IOException ioEx)
        {
            string message = ioEx.Message.ToLower();
            return message.Contains("device attached to the system is not functioning") ||
                   message.Contains("device is not functioning") ||
                   message.Contains("device has been removed") ||
                   message.Contains("device not ready") ||
                   message.Contains("port does not exist") ||
                   message.Contains("handle is invalid") ||
                   message.Contains("operation was canceled");
        }
        
        private void HandleDeviceDisconnection(IOException ioEx)
        {
            Connection.OnDataReceived("[DEVICE DISCONNECTED] The connected device has been removed or is not functioning");
            Connection.OnDataReceived($"[ERROR DETAILS] {ioEx.Message}");
            Connection.OnDataReceived("[INFO] Please check the device connection and try reconnecting");
            Connection.OnDataReceived("[INFO] The COM port list will be refreshed automatically");
            
            // Set connection status to error with specific message
            Connection.SetStatus(ConnectionStatus.Error);
            Connection.SetError("Device disconnected - check hardware connection");
            
            // Request port list update to refresh available ports
            PortConnection.RequestPortListUpdate();
            
            // Clean up the serial port
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.Close();
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
            finally
            {
                _serialPort?.Dispose();
                _serialPort = null;
            }
        }
        
        private string GetDetailedErrorMessage(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException uae => GetUnauthorizedAccessMessage(uae),
                IOException ioe => GetIOErrorMessage(ioe),
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
                catch (IOException ioEx)
                {
                    // Handle device disconnection during write
                    if (IsDeviceDisconnectedError(ioEx))
                    {
                        HandleDeviceDisconnection(ioEx);
                        throw new InvalidOperationException("Device was disconnected during write operation");
                    }
                    
                    var error = $"IO error sending command: {ioEx.Message}";
                    Connection.OnDataReceived($"Error: {error}");
                    throw new IOException(error, ioEx);
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
            catch (IOException ioEx)
            {
                // Handle device disconnection during read
                if (IsDeviceDisconnectedError(ioEx))
                {
                    HandleDeviceDisconnection(ioEx);
                    return;
                }
                
                Connection.OnDataReceived($"[READ ERROR] IO error: {ioEx.Message}");
                Connection.SetError($"Port read error: {ioEx.Message}");
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