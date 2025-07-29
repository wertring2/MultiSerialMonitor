@echo off
REM Batch Script to Build Multi Serial Monitor and Create Installer
REM Q WAVE COMPANY LIMITED
REM Version 1.0.0

echo ========================================
echo Multi Serial Monitor Installer Builder
echo Q WAVE COMPANY LIMITED
echo ========================================
echo.

REM Check if PowerShell is available
where powershell >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: PowerShell not found. Please install PowerShell.
    pause
    exit /b 1
)

REM Check if dotnet is available
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found. Please install .NET 9 SDK.
    echo Download from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Starting build process...
echo.

REM Run PowerShell script with execution policy bypass
powershell -ExecutionPolicy Bypass -File "build-installer.ps1" %*

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Build process failed.
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo.
pause