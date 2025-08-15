# Smart Audio Auto-Leveler

A modern Windows desktop application that automatically adjusts background application volumes based on the foreground application. Built with C# .NET 8, WPF, and NAudio.

## Features

- **Automatic Volume Control**: Instantly lowers background app volumes when switching to a different application
- **Real-time Detection**: Uses Windows CoreAudio APIs and Win32 hooks for instant foreground app detection
- **Modern UI**: Beautiful WPF interface with adaptive light/dark themes and rounded corners
- **System Tray Integration**: Runs quietly in the background with easy access via system tray
- **Configurable Settings**: Adjustable background volume percentage and app exclusion list
- **Autostart Support**: Option to start with Windows
- **MSIX Packaging**: Modern Windows app packaging for easy installation and updates

## Screenshots

The application features a modern, clean interface with:
- Status overview showing system health
- Quick action buttons
- Recent activity log
- Comprehensive settings window
- System tray integration

## System Requirements

- Windows 10 (version 17763.0) or later
- .NET 8 Runtime (included in MSIX package)
- Audio output device
- Administrator privileges (for audio session control)

## Installation

### Option 1: MSIX Package (Recommended)
1. Download the latest MSIX package from releases
2. Double-click the MSIX file to install
3. Follow the installation prompts
4. Launch from Start Menu or Desktop shortcut

### Option 2: Build from Source
1. Clone this repository
2. Ensure you have .NET 8 SDK installed
3. Open the solution in Visual Studio 2022 or later
4. Build and run the project

## Building from Source

### Prerequisites
- Visual Studio 2022 17.8+ or .NET 8 SDK
- Windows 10/11 development environment
- Git

### Build Steps
```bash
# Clone the repository
git clone https://github.com/yourusername/SmartAudioAutoLeveler.git
cd SmartAudioAutoLeveler

# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Run the application
dotnet run --project SmartAudioAutoLeveler
```

### Creating MSIX Package
```bash
# Publish for MSIX packaging
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true

# The MSIX package will be created in the publish directory
```

## Usage

### First Run
1. Launch the application
2. Grant necessary permissions when prompted
3. The app will start monitoring audio sessions automatically
4. Access settings via the system tray icon or main window

### Basic Operation
- **Background Volume**: Adjust the percentage in settings (default: 50%)
- **App Exclusions**: Add applications that should not have their volume adjusted
- **System Tray**: Right-click for quick access to settings and controls
- **Autostart**: Enable to start the app automatically with Windows

### Settings Configuration
- **Volume Settings**: Control background app volume reduction
- **Application Exclusions**: Specify apps to ignore
- **General Settings**: Autostart, minimize to tray, notifications
- **Theme**: Choose between Light, Dark, or System themes

## Architecture

### Core Components
- **AudioManager**: Handles CoreAudio session management and volume control
- **ForegroundAppDetector**: Monitors foreground window changes using Win32 hooks
- **SettingsManager**: Manages JSON-based configuration persistence
- **TrayIconManager**: System tray integration and context menu
- **MainWindow**: Main application interface with status monitoring
- **SettingsWindow**: Comprehensive settings configuration

### Key Technologies
- **NAudio**: Audio session enumeration and volume control
- **Windows CoreAudio APIs**: Direct audio session management
- **Win32 Hooks**: Real-time foreground window detection
- **WPF**: Modern user interface with XAML
- **.NET 8**: Latest .NET framework with performance optimizations

## Configuration

### Settings File Location
Settings are stored in: `%APPDATA%\SmartAudioAutoLeveler\settings.json`

### Sample Configuration
```json
{
  "backgroundVolume": 0.5,
  "excludedApps": ["Spotify", "Discord"],
  "autostart": false,
  "minimizeToTray": true,
  "startMinimized": true,
  "theme": "System",
  "enableNotifications": true
}
```

## Troubleshooting

### Common Issues

**Audio not being detected**
- Ensure the application has necessary permissions
- Check that audio is playing in other applications
- Verify Windows audio services are running

**Volume not adjusting**
- Check if apps are in the exclusion list
- Verify background volume setting is not 100%
- Ensure the app is running with appropriate privileges

**App not starting**
- Check Windows Event Viewer for errors
- Verify .NET 8 runtime is installed
- Check antivirus software isn't blocking the application

### Performance
- The application typically uses <1% CPU when idle
- Memory usage is minimal (<50MB)
- Audio processing is event-driven, not polling-based

## Development

### Project Structure
```
SmartAudioAutoLeveler/
├── AudioManager.cs           # Core audio management
├── ForegroundAppDetector.cs  # Foreground app detection
├── SettingsManager.cs        # Configuration management
├── TrayIconManager.cs        # System tray integration
├── MainWindow.xaml           # Main application window
├── SettingsWindow.xaml       # Settings interface
├── Package.appxmanifest      # MSIX package manifest
└── Properties/               # Build configuration
```

### Adding Features
1. Follow the existing code patterns
2. Add proper error handling and logging
3. Update the settings model if needed
4. Test thoroughly with different audio scenarios

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- **NAudio**: Audio processing library
- **Windows CoreAudio**: Microsoft's audio architecture
- **WPF Community**: UI framework and styling inspiration

## Support

For issues, questions, or feature requests:
- Create an issue on GitHub
- Check the troubleshooting section
- Review the configuration examples

## Version History

### v1.0.0
- Initial release
- Core audio management functionality
- Modern WPF interface
- System tray integration
- MSIX packaging support
- Light/dark theme support
- Comprehensive settings management

---

**Note**: This application requires Windows 10 or later and may need administrator privileges for full audio control functionality.
