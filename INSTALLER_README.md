# Multi Serial Monitor - Installer Guide

## Overview
This guide explains how to build and create installers for Multi Serial Monitor application.

## Prerequisites

### Required Software
1. **.NET 9 SDK** - Download from [Microsoft .NET](https://dotnet.microsoft.com/download)
2. **NSIS (Nullsoft Scriptable Install System)** - Download from [NSIS Official Site](https://nsis.sourceforge.io/)
   - Required only for creating the Windows installer (.exe)
   - The application itself can run without NSIS

### System Requirements
- Windows 10 or later (64-bit)
- .NET 9.0 Runtime (included in single-file deployment)

## Building the Installer

### Method 1: Using Batch Script (Recommended for Windows Users)
```cmd
# Double-click or run from command prompt
build-installer.bat
```

### Method 2: Using PowerShell Script
```powershell
# Run from PowerShell
.\build-installer.ps1

# With options
.\build-installer.ps1 -Configuration Release -Platform win-x64 -Verbose
```

### Method 3: Manual Build Process
```cmd
# 1. Clean and restore
dotnet clean --configuration Release
dotnet restore

# 2. Build the application
dotnet build --configuration Release

# 3. Publish as single-file
dotnet publish --configuration Release --runtime win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

# 4. Create installer with NSIS (if installed)
makensis installer.nsi
```

## Output Files

After successful build, you will find these files in the `installer` directory:

### Single-File Executable
- **MultiSerialMonitor.exe** - Standalone executable (~50-80 MB)
  - Contains all dependencies
  - No installation required
  - Can be run directly

### Windows Installer
- **MultiSerialMonitor_Setup_v1.0.0.exe** - Full installer (~50-80 MB)
  - Professional installation experience
  - Creates Start Menu shortcuts
  - Adds to Programs and Features
  - Includes uninstaller

## Installer Features

### Installation Options
- **Main Application** (Required) - Core Multi Serial Monitor files
- **Desktop Shortcut** - Create shortcut on desktop
- **Start Menu Shortcuts** - Add to Start Menu under "Q WAVE"
- **Quick Launch Shortcut** - Add to Quick Launch toolbar

### Installation Locations
- **Default**: `C:\Program Files\Q WAVE\Multi Serial Monitor\`
- **Customizable**: User can choose different location during installation

### Registry Entries
The installer creates the following registry entries:
- Application settings: `HKCU\Software\QWAVE\MultiSerialMonitor`
- Uninstall information: `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiSerialMonitor`

### Uninstallation
The installer includes a complete uninstaller that:
- Removes all application files
- Cleans up registry entries
- Removes shortcuts
- Optionally removes user data and settings

## Distribution

### Single-File Executable
- Ideal for portable usage
- No installation required
- Can be distributed via USB, email, or download
- Users just run the .exe file

### Windows Installer
- Professional deployment
- Better for enterprise environments
- Handles Windows integration (shortcuts, uninstall)
- Provides installation/uninstallation tracking

## Troubleshooting

### Build Issues
1. **"dotnet not found"**
   - Install .NET 9 SDK from Microsoft
   - Restart command prompt/PowerShell after installation

2. **"Access denied" errors**
   - Run command prompt as Administrator
   - Check antivirus software blocking the build

3. **"NSIS not found"**
   - Install NSIS from the official website
   - The single-file executable will still be created
   - Only the installer creation will be skipped

### Runtime Issues
1. **"Application won't start"**
   - Ensure Windows is 64-bit
   - Check Windows version compatibility (Windows 10+)
   - Try running as Administrator

2. **"Missing dependencies"**
   - Use the single-file version (all dependencies included)
   - Or install .NET 9 Runtime separately

## File Structure
```
MultiSerialMonitor/
├── MultiSerialMonitor.exe         # Main application
├── QW LOGO Qwave.png              # Company logo
├── favicon.ico                    # Application icon
├── LICENSE.txt                    # End User License Agreement
├── installer.nsi                  # NSIS installer script
├── build-installer.ps1            # PowerShell build script
├── build-installer.bat            # Batch build script
└── installer/                     # Output directory
    ├── MultiSerialMonitor.exe     # Single-file executable
    └── MultiSerialMonitor_Setup_v1.0.0.exe  # Windows installer
```

## Technical Details

### Single-File Publishing
- Uses .NET 9's single-file publishing feature
- Includes all dependencies and runtime
- Compressed to reduce file size
- Extracted to temp directory at runtime

### Installer Technology
- **NSIS**: Proven installer technology
- **Unicode support**: Handles international characters
- **Modern UI**: Professional appearance
- **Compression**: LZMA compression for smaller installer size

## Support

For technical support or questions:
- **Company**: Q WAVE COMPANY LIMITED
- **Website**: https://qwave.co.th
- **Email**: info@qwave.co.th

---
© 2025 Q WAVE COMPANY LIMITED. All rights reserved.