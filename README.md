# Multi Serial Monitor

A .NET 9 Windows Forms application for monitoring multiple Serial Ports and Telnet connections simultaneously.

## Features

- **Multi-Port Monitoring**: Monitor multiple serial ports and Telnet connections concurrently
- **Dashboard View**: Compact view showing all active ports with their last received line
- **Console View**: Full console view for each port with complete output history
- **Real-time Updates**: Live data monitoring with visual status indicators
- **Command Sending**: Send commands to connected ports through the console view
- **Connection Management**: Easy add/remove/connect/disconnect functionality

## Architecture

### Core Components

1. **Models**
   - `PortConnection`: Represents a serial port or Telnet connection configuration

2. **Services**
   - `IPortMonitor`: Interface for port monitoring
   - `SerialPortMonitor`: Handles serial port communication
   - `TelnetMonitor`: Handles Telnet protocol communication

3. **UI Components**
   - `Form1`: Main dashboard displaying all port panels
   - `PortPanel`: Custom control showing port summary in dashboard
   - `ConsoleForm`: Full console view for a specific port
   - `AddPortForm`: Dialog for adding new connections

## Usage

1. **Adding a Port**:
   - Click "Add Port" in the toolbar
   - Choose between Serial Port or Telnet
   - Configure connection settings
   - Click OK to add and auto-connect

2. **Viewing Console**:
   - Click "Expand" on any port panel to open the console view
   - View complete output history
   - Send commands using the input field

3. **Managing Connections**:
   - Right-click on any port panel for context menu
   - Connect/Disconnect/Remove ports as needed
   - Click the × button on each port panel to quickly remove it
   - Use "Remove All" button in toolbar to clear all ports
   - Press Delete key when a port panel is focused to remove it

## Requirements

- .NET 9.0 SDK
- Windows 10/11
- Serial ports or Telnet servers to connect to

## NuGet Dependencies

- `System.IO.Ports` (8.0.0) - For serial port communication
- `Telnet` (0.11.3) - For Telnet protocol support

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

Or open in Visual Studio and press F5.