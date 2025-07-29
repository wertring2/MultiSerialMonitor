# PowerShell Script to Build Multi Serial Monitor and Create Installer
# Q WAVE COMPANY LIMITED
# Version 1.0.0

param(
    [string]$Configuration = "Release",
    [string]$Platform = "win-x64",
    [switch]$SkipBuild = $false,
    [switch]$Verbose = $false
)

# Color functions
function Write-ColorOutput {
    param(
        [Parameter(Mandatory)]
        [String] $Message,
        [ConsoleColor] $ForegroundColor = [ConsoleColor]::White
    )
    
    $currentForeground = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $Host.UI.RawUI.ForegroundColor = $currentForeground
}

Write-ColorOutput "========================================" -ForegroundColor Cyan
Write-ColorOutput "Multi Serial Monitor Installer Builder" -ForegroundColor Cyan
Write-ColorOutput "Q WAVE COMPANY LIMITED" -ForegroundColor Yellow
Write-ColorOutput "========================================" -ForegroundColor Cyan
Write-Output ""

# Check if dotnet is available
if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-ColorOutput "ERROR: .NET SDK not found. Please install .NET 9 SDK." -ForegroundColor Red
    exit 1
}

# Get project directory
$projectDir = Get-Location
$publishDir = Join-Path $projectDir "bin\$Configuration\net9.0-windows\$Platform\publish"
$installerDir = Join-Path $projectDir "installer"

Write-ColorOutput "Project Directory: $projectDir" -ForegroundColor Green
Write-ColorOutput "Publish Directory: $publishDir" -ForegroundColor Green
Write-ColorOutput "Installer Directory: $installerDir" -ForegroundColor Green
Write-Output ""

if (-not $SkipBuild) {
    Write-ColorOutput "Step 1: Cleaning previous builds..." -ForegroundColor Yellow
    try {
        dotnet clean --configuration $Configuration --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw "Clean failed" }
        Write-ColorOutput "✓ Clean completed successfully" -ForegroundColor Green
    }
    catch {
        Write-ColorOutput "✗ Clean failed: $_" -ForegroundColor Red
        exit 1
    }

    Write-ColorOutput "Step 2: Restoring NuGet packages..." -ForegroundColor Yellow
    try {
        dotnet restore --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw "Restore failed" }
        Write-ColorOutput "✓ Restore completed successfully" -ForegroundColor Green
    }
    catch {
        Write-ColorOutput "✗ Restore failed: $_" -ForegroundColor Red
        exit 1
    }

    Write-ColorOutput "Step 3: Building application..." -ForegroundColor Yellow
    try {
        if ($Verbose) {
            dotnet build --configuration $Configuration --verbosity normal
        } else {
            dotnet build --configuration $Configuration --verbosity minimal
        }
        if ($LASTEXITCODE -ne 0) { throw "Build failed" }
        Write-ColorOutput "✓ Build completed successfully" -ForegroundColor Green
    }
    catch {
        Write-ColorOutput "✗ Build failed: $_" -ForegroundColor Red
        exit 1
    }

    Write-ColorOutput "Step 4: Publishing single-file application..." -ForegroundColor Yellow
    try {
        $publishArgs = @(
            "publish"
            "--configuration", $Configuration
            "--runtime", $Platform
            "--self-contained", "true"
            "--output", $publishDir
            "/p:PublishSingleFile=true"
            "/p:IncludeNativeLibrariesForSelfExtract=true"
            "/p:IncludeAllContentForSelfExtract=true"
            "/p:EnableCompressionInSingleFile=true"
            "--verbosity", $(if ($Verbose) { "normal" } else { "minimal" })
        )
        
        & dotnet @publishArgs
        if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
        Write-ColorOutput "✓ Publish completed successfully" -ForegroundColor Green
    }
    catch {
        Write-ColorOutput "✗ Publish failed: $_" -ForegroundColor Red
        exit 1
    }
}

Write-ColorOutput "Step 5: Preparing installer directory..." -ForegroundColor Yellow
try {
    # Create installer directory
    if (Test-Path $installerDir) {
        Remove-Item -Path $installerDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $installerDir -Force | Out-Null

    # Copy published files
    Copy-Item -Path "$publishDir\MultiSerialMonitor.exe" -Destination $installerDir -Force
    Copy-Item -Path "$projectDir\QW LOGO Qwave.png" -Destination $installerDir -Force
    Copy-Item -Path "$projectDir\favicon.ico" -Destination $installerDir -Force
    Copy-Item -Path "$projectDir\LICENSE.txt" -Destination $installerDir -Force
    Copy-Item -Path "$projectDir\installer.nsi" -Destination $installerDir -Force

    Write-ColorOutput "✓ Installer directory prepared" -ForegroundColor Green
}
catch {
    Write-ColorOutput "✗ Failed to prepare installer directory: $_" -ForegroundColor Red
    exit 1
}

# Get file sizes for information
$exeSize = (Get-Item "$installerDir\MultiSerialMonitor.exe").Length
$exeSizeMB = [math]::Round($exeSize / 1MB, 2)

Write-Output ""
Write-ColorOutput "Build Summary:" -ForegroundColor Cyan
Write-ColorOutput "-------------------------------------" -ForegroundColor Cyan
Write-ColorOutput "- Configuration: $Configuration" -ForegroundColor White
Write-ColorOutput "- Platform: $Platform" -ForegroundColor White
Write-ColorOutput "- Single-file executable size: $exeSizeMB MB" -ForegroundColor White
Write-ColorOutput "- Output location: $installerDir" -ForegroundColor White

Write-Output ""
Write-ColorOutput "Step 6: Creating NSIS installer..." -ForegroundColor Yellow

# Check if NSIS is available
$nsisPath = $null
$possiblePaths = @(
    "${env:ProgramFiles}\NSIS\makensis.exe",
    "${env:ProgramFiles(x86)}\NSIS\makensis.exe",
    "C:\Program Files\NSIS\makensis.exe",
    "C:\Program Files (x86)\NSIS\makensis.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $nsisPath = $path
        break
    }
}

if (-not $nsisPath) {
    Write-ColorOutput "WARNING: NSIS not found. Installer .exe will not be created." -ForegroundColor Yellow
    Write-ColorOutput "To create the installer, install NSIS from: https://nsis.sourceforge.io/" -ForegroundColor Yellow
    Write-ColorOutput "Then run: `"$nsisPath`" `"$installerDir\installer.nsi`"" -ForegroundColor Yellow
} else {
    try {
        Push-Location $installerDir
        & $nsisPath "installer.nsi"
        if ($LASTEXITCODE -ne 0) { throw "NSIS compilation failed" }
        Pop-Location
        
        $installerFile = Get-ChildItem -Path $installerDir -Filter "MultiSerialMonitor_Setup_*.exe" | Select-Object -First 1
        if ($installerFile) {
            $installerSize = [math]::Round($installerFile.Length / 1MB, 2)
            Write-ColorOutput "✓ Installer created successfully" -ForegroundColor Green
            Write-ColorOutput "- Installer file: $($installerFile.Name)" -ForegroundColor White
            Write-ColorOutput "- Installer size: $installerSize MB" -ForegroundColor White
        }
    }
    catch {
        Write-ColorOutput "✗ Failed to create installer: $_" -ForegroundColor Red
        Pop-Location
        exit 1
    }
}

Write-Output ""
Write-ColorOutput "========================================" -ForegroundColor Cyan
Write-ColorOutput "BUILD COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-ColorOutput "========================================" -ForegroundColor Cyan

Write-Output ""
Write-ColorOutput "Files created:" -ForegroundColor Yellow
Write-ColorOutput "- Single-file executable: MultiSerialMonitor.exe ($exeSizeMB MB)" -ForegroundColor White
if ($nsisPath -and (Test-Path "$installerDir\\MultiSerialMonitor_Setup_*.exe")) {
    $installerFile = Get-ChildItem -Path $installerDir -Filter "MultiSerialMonitor_Setup_*.exe" | Select-Object -First 1
    $installerSize = [math]::Round($installerFile.Length / 1MB, 2)
    Write-ColorOutput "- Windows installer: $($installerFile.Name) ($installerSize MB)" -ForegroundColor White
}

Write-Output ""
Write-ColorOutput "Location: $installerDir" -ForegroundColor Green
Write-Output ""

# Open installer directory
if (Get-Command "explorer" -ErrorAction SilentlyContinue) {
    Write-ColorOutput "Opening installer directory..." -ForegroundColor Yellow
    Start-Process "explorer" -ArgumentList $installerDir
}

Write-ColorOutput "Build script completed!" -ForegroundColor Green