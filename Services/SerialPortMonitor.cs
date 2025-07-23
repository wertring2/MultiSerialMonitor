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
            await Task.Run(() =>
            {
                try
                {
                    Connection.SetStatus(ConnectionStatus.Connecting);
                    
                    _serialPort = new SerialPort
                    {
                        PortName = Connection.PortName,
                        BaudRate = Connection.BaudRate,
                        Parity = Connection.Parity,
                        DataBits = Connection.DataBits,
                        StopBits = Connection.StopBits,
                        ReadTimeout = 500,
                        WriteTimeout = 500
                    };
                    
                    _serialPort.DataReceived += OnSerialDataReceived;
                    _serialPort.ErrorReceived += OnSerialErrorReceived;
                    
                    _serialPort.Open();
                    Connection.SetStatus(ConnectionStatus.Connected);
                }
                catch (Exception ex)
                {
                    Connection.SetStatus(ConnectionStatus.Error);
                    Connection.OnDataReceived($"Error: {ex.Message}");
                    throw;
                }
            });
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
                        if (!string.IsNullOrEmpty(line))
                        {
                            Connection.OnDataReceived(line);
                        }
                        _lineBuilder.Clear();
                    }
                    else
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
            DisconnectAsync().Wait();
        }
    }
}