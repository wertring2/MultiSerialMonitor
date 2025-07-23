# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MultiSerialMonitor is a .NET 9 Windows Forms application designed for monitoring multiple Serial Ports and Telnet connections simultaneously. It features a dashboard view for multiple connections and individual console views for detailed interaction.

## Build and Run Commands

```bash
# Build the project
dotnet build

# Run the application
dotnet run

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

Note: On WSL, use the Windows dotnet.exe path:
```bash
/mnt/c/Program\ Files/dotnet/dotnet.exe build
```

## Architecture and Key Design Patterns

### Service Layer Pattern
The application uses a service layer with interfaces to abstract communication protocols:
- `IPortMonitor` interface defines the contract for all port monitors
- `SerialPortMonitor` and `TelnetMonitor` implement this interface
- This allows easy addition of new communication protocols

### Event-Driven Architecture
- `PortConnection` model fires events (`DataReceived`, `StatusChanged`) when data arrives or connection status changes
- UI components subscribe to these events for real-time updates
- Cross-thread marshaling is handled using `InvokeRequired` pattern

### UI Component Hierarchy
```
Form1 (Main Dashboard)
├── ToolStrip (Add Port, Refresh buttons)
├── FlowLayoutPanel (_portsPanel)
│   └── PortPanel[] (Custom control for each connection)
└── StatusStrip

ConsoleForm (Detailed View)
├── RichTextBox (Console output)
├── TextBox + Button (Command input)
└── ToolStrip (Connect/Disconnect/Clear)
```

### Connection Management
- Each connection has a unique ID (GUID)
- Three dictionaries in Form1 maintain relationships:
  - `_monitors`: Maps connection ID to IPortMonitor instance
  - `_portPanels`: Maps connection ID to PortPanel control
  - `_consoleForms`: Maps connection ID to ConsoleForm (if open)

### Data Flow
1. Data arrives at SerialPortMonitor/TelnetMonitor via hardware/network
2. Monitor parses data into lines and calls `Connection.OnDataReceived()`
3. PortConnection fires `DataReceived` event
4. UI components (PortPanel, ConsoleForm) update via event handlers
5. Cross-thread calls are marshaled to UI thread using `Invoke()`

## Important Implementation Details

### Timestamp Handling
The system was recently updated to preserve original timestamps in data:
- `PortConnection.OnDataReceived()` checks if data contains timestamp patterns `[...]`
- If timestamps exist, original format is preserved
- Otherwise, a timestamp is prepended

### Line Building
Both monitors build complete lines before forwarding:
- Characters are accumulated in a StringBuilder
- Lines are completed on `\n` character
- Carriage returns (`\r`) are trimmed/ignored
- Empty lines are skipped

### Connection Lifecycle
1. User adds connection via AddPortForm
2. Monitor is created and auto-connects
3. Connection can be managed via context menu (connect/disconnect/remove)
4. Console forms are opened on-demand and tracked
5. Proper cleanup on form closing disposes all monitors

### Dashboard Display Optimization
PortPanel dynamically adjusts text display:
- Attempts to show full line content
- Falls back to smaller font (8pt) if needed
- Truncates intelligently if still too long
- Label height increased to accommodate wrapped text

## Key Files to Understand

- `Models/PortConnection.cs` - Core data model and event handling
- `Services/IPortMonitor.cs` - Service interface definition
- `Form1.cs` - Main dashboard logic and connection management
- `Controls/PortPanel.cs` - Dashboard panel implementation
- `Forms/ConsoleForm.cs` - Detailed console view implementation

## Common Tasks

### Adding a New Communication Protocol
1. Create new class implementing `IPortMonitor`
2. Add new ConnectionType enum value
3. Update AddPortForm with new tab
4. Update Form1.AddPortConnectionAsync() to instantiate new monitor type

### Modifying Dashboard Display
- Edit `PortPanel.InitializeComponents()` for layout changes
- Modify `OnDataReceived()` for display logic
- Adjust panel Height property for size changes

### Changing Console Appearance
- Edit `ConsoleForm.InitializeComponents()`
- Modify color logic in `OnDataReceived()`
- Adjust RichTextBox properties for font/colors