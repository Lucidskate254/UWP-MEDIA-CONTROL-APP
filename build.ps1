# Smart Audio Auto-Leveler Build Script
# PowerShell version for better cross-platform support

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$Clean,
    [switch]$Restore,
    [switch]$Build,
    [switch]$Publish,
    [switch]$All
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Colors for output
$Colors = @{
    Info = "Cyan"
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
}

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Info { Write-ColorOutput $args[0] $Colors.Info }
function Write-Success { Write-ColorOutput $args[0] $Colors.Success }
function Write-Warning { Write-ColorOutput $args[0] $Colors.Warning }
function Write-Error { Write-ColorOutput $args[0] $Colors.Error }

# Header
Write-ColorOutput "===============================================" "Cyan"
Write-ColorOutput "Smart Audio Auto-Leveler Build Script" "Cyan"
Write-ColorOutput "===============================================" "Cyan"
Write-Host ""

# Check if .NET 8 SDK is installed
Write-Info "Checking .NET 8 SDK installation..."
try {
    $dotnetVersion = dotnet --version
    Write-Success ".NET SDK version: $dotnetVersion"
} catch {
    Write-Error "Error: .NET 8 SDK is not installed or not in PATH"
    Write-Info "Please install .NET 8 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

# Check if we're on Windows
if ($Runtime -like "win-*" -and $PSVersionTable.Platform -ne "Win32NT") {
    Write-Warning "Warning: Building for Windows runtime on non-Windows platform"
}

Write-Host ""

# Default to all if no specific actions specified
if (-not ($Clean -or $Restore -or $Build -or $Publish)) {
    $All = $true
}

# Clean
if ($Clean -or $All) {
    Write-Info "Cleaning previous builds..."
    try {
        dotnet clean --configuration $Configuration
        Write-Success "Clean completed successfully"
    } catch {
        Write-Warning "Warning: Clean failed, continuing..."
    }
    Write-Host ""
}

# Restore
if ($Restore -or $All) {
    Write-Info "Restoring dependencies..."
    try {
        dotnet restore
        Write-Success "Dependencies restored successfully"
    } catch {
        Write-Error "Error: Failed to restore dependencies"
        exit 1
    }
    Write-Host ""
}

# Build
if ($Build -or $All) {
    Write-Info "Building project..."
    try {
        dotnet build --configuration $Configuration --no-restore
        Write-Success "Build completed successfully"
    } catch {
        Write-Error "Error: Build failed"
        exit 1
    }
    Write-Host ""
}

# Publish
if ($Publish -or $All) {
    Write-Info "Publishing for MSIX packaging..."
    try {
        $publishArgs = @(
            "publish",
            "-c", $Configuration,
            "-r", $Runtime,
            "--self-contained", "true",
            "-p:PublishSingleFile=true",
            "-p:PublishReadyToRun=true",
            "--no-build"
        )
        
        dotnet $publishArgs
        Write-Success "Publish completed successfully"
    } catch {
        Write-Error "Error: Publish failed"
        exit 1
    }
    Write-Host ""
}

# Success message
Write-ColorOutput "===============================================" "Green"
Write-Success "Build completed successfully!"
Write-ColorOutput "===============================================" "Green"
Write-Host ""

# Output information
$publishPath = "SmartAudioAutoLeveler\bin\$Configuration\net8.0-windows\publish"
Write-Info "Output files:"
Write-Info "- Executable: $publishPath\SmartAudioAutoLeveler.exe"
Write-Info "- MSIX ready files: $publishPath\"
Write-Host ""

if ($Runtime -like "win-*") {
    Write-Info "To create MSIX package:"
    Write-Info "1. Use Visual Studio's MSIX packaging tools, or"
    Write-Info "2. Use the MakeAppx tool from Windows SDK, or"
    Write-Info "3. Use the MSIX Packaging Tool from Microsoft Store"
    Write-Host ""
}

Write-Info "Build script completed successfully!"
Write-Host ""
