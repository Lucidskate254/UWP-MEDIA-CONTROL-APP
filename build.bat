@echo off
echo Building Smart Audio Auto-Leveler...
echo.

REM Check if .NET 8 SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Error: .NET 8 SDK is not installed or not in PATH
    echo Please install .NET 8 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo .NET SDK version:
dotnet --version
echo.

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean --configuration Release
if %errorlevel% neq 0 (
    echo Warning: Clean failed, continuing...
)

REM Restore dependencies
echo Restoring dependencies...
dotnet restore
if %errorlevel% neq 0 (
    echo Error: Failed to restore dependencies
    pause
    exit /b 1
)

REM Build the project
echo Building project...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo Error: Build failed
    pause
    exit /b 1
)

REM Publish for MSIX packaging
echo Publishing for MSIX packaging...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true --no-build
if %errorlevel% neq 0 (
    echo Error: Publish failed
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo.
echo Output files:
echo - Executable: SmartAudioAutoLeveler\bin\Release\net8.0-windows\publish\SmartAudioAutoLeveler.exe
echo - MSIX ready files: SmartAudioAutoLeveler\bin\Release\net8.0-windows\publish\
echo.
echo To create MSIX package, use the MakeAppx tool or Visual Studio's MSIX packaging tools.
echo.

pause
