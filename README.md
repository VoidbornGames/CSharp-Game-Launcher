# Game Launcher

A Steam-like game launcher built with C# and WPF that provides downloading, updating, and installing games and DLC.

## Features

### Core Functionality
- **Game Library Management**: Browse and manage your game collection
- **Download & Install**: Download and install games with progress tracking
- **Update System**: Automatic game updates with version management
- **DLC Support**: Download and install downloadable content
- **Search & Filter**: Find games quickly with search functionality
- **Game Launching**: Launch installed games directly from the launcher

### User Interface
- **Modern Dark Theme**: Steam-inspired dark UI design
- **Responsive Layout**: Adaptive grid layout for game tiles
- **Progress Tracking**: Visual progress bars for downloads and installations
- **Game Details**: Detailed game information and screenshots
- **Status Indicators**: Clear status indicators for installed, downloading, and available games

### Technical Features
- **MVVM Architecture**: Clean separation of concerns using MVVM pattern
- **Async Operations**: Non-blocking UI with async/await patterns
- **Data Persistence**: JSON-based configuration storage
- **Error Handling**: Robust error handling throughout the application
- **Modular Design**: Well-organized code structure with services and models

## Architecture

### Project Structure
```
GameLauncher/
├── Models/
│   ├── Game.cs              # Game data model
│   └── DLC.cs               # DLC data model
├── Services/
│   └── GameService.cs       # Game management service
├── ViewModels/
│   └── MainViewModel.cs     # Main window view model
├── Views/
│   └── MainWindow.xaml      # Main application window
├── Converters/
│   └── BooleanToVisibilityConverter.cs  # XAML converters
└── App.xaml                 # Application entry point
```

### Key Components

#### Models
- **Game**: Represents a game with properties like title, description, version, install status, etc.
- **DLC**: Represents downloadable content with pricing and installation status

#### Services
- **GameService**: Handles all game-related operations including:
  - Downloading and installing games
  - Updating existing installations
  - Managing DLC
  - Launching games
  - Persisting game configurations

#### ViewModels
- **MainViewModel**: Manages the main window state and commands using MVVM pattern

## Getting Started

### Prerequisites
- .NET 8.0 or later
- Visual Studio 2022 or Visual Studio Code
- Windows 10/11

### Building the Application
1. Open the solution in Visual Studio
2. Restore NuGet packages
3. Build the solution (Ctrl+Shift+B)
4. Run the application (F5)

### Usage
1. **Browse Games**: View available games in the main library
2. **Search**: Use the search box to find specific games
3. **Install Games**: Click "Install" on any game to download and install it
4. **Launch Games**: Click "Play" on installed games to launch them
5. **Manage DLC**: View and install available DLC in the details panel
6. **Update Games**: Use the "Update Game" button to get the latest version

## Configuration

The launcher stores its configuration in:
- **Games Directory**: `%USERPROFILE%\Documents\GameLauncher\Games\`
- **Config File**: `%USERPROFILE%\Documents\GameLauncher\config.json`

## Customization

### Adding New Games
To add new games to the launcher, modify the `GetAvailableGamesAsync()` method in `GameService.cs`. In a production environment, this would typically fetch from a web API.

### Styling
The application uses a dark theme similar to Steam. Colors and styles can be customized in the XAML files.

### Extending Functionality
The modular architecture makes it easy to add new features:
- Add new models for additional data types
- Extend services for new functionality
- Create new views and view models for additional screens

## Dependencies

- **Newtonsoft.Json**: JSON serialization for configuration
- **Microsoft.Toolkit.Mvvm**: MVVM helpers and commands
- **System.IO.Compression**: File compression support
- **System.Net.Http**: HTTP client for downloads

## Future Enhancements

- **Cloud Integration**: Sync game library across devices
- **Achievement System**: Track and display game achievements
- **Social Features**: Friends list and game sharing
- **Mod Support**: Install and manage game modifications
- **Backup System**: Backup and restore game saves
- **Multiple Platforms**: Support for different game platforms

## License

This project is provided as an educational example. Feel free to use and modify as needed for your own projects.
