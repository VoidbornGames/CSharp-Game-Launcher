using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using GameLauncher.Models;
using Newtonsoft.Json;

namespace GameLauncher.Services
{
    public class GameService
    {
        private readonly HttpClient _httpClient;
        private readonly string _gamesDirectory;
        private readonly string _configPath;

        public GameService()
        {
            _httpClient = new HttpClient();
            _gamesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GameLauncher", "Games");
            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GameLauncher", "config.json");
            
            Directory.CreateDirectory(_gamesDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        }

        public async Task<List<Game>> GetAvailableGamesAsync()
        {
            // In a real implementation, this would fetch from a server
            // For demo purposes, return sample games
            return new List<Game>
            {
                new Game
                {
                    Id = 1,
                    Title = "Adventure Quest",
                    Description = "An epic adventure game with stunning graphics and immersive gameplay.",
                    Version = "1.2.3",
                    SizeInBytes = 2147483648, // 2GB
                    ImageUrl = "https://images.pexels.com/photos/442576/pexels-photo-442576.jpeg",
                    DownloadUrl = "https://example.com/games/adventure-quest.zip",
                    ReleaseDate = DateTime.Now.AddMonths(-6),
                    Tags = new List<string> { "Adventure", "RPG", "Single Player" },
                    DLCs = new List<DLC>
                    {
                        new DLC
                        {
                            Id = 1,
                            GameId = 1,
                            Name = "Dragon Expansion",
                            Description = "Add dragons and new quests to your adventure!",
                            SizeInBytes = 524288000, // 500MB
                            Price = 9.99m,
                            DownloadUrl = "https://example.com/dlc/dragon-expansion.zip"
                        }
                    }
                },
                new Game
                {
                    Id = 2,
                    Title = "Space Explorer",
                    Description = "Explore the vast universe in this space simulation game.",
                    Version = "2.1.0",
                    SizeInBytes = 3221225472, // 3GB
                    ImageUrl = "https://images.pexels.com/photos/586063/pexels-photo-586063.jpeg",
                    DownloadUrl = "https://example.com/games/space-explorer.zip",
                    ReleaseDate = DateTime.Now.AddMonths(-3),
                    Tags = new List<string> { "Simulation", "Space", "Multiplayer" }
                },
                new Game
                {
                    Id = 3,
                    Title = "Racing Championship",
                    Description = "High-speed racing action with realistic physics.",
                    Version = "1.0.5",
                    SizeInBytes = 1610612736, // 1.5GB
                    ImageUrl = "https://images.pexels.com/photos/358070/pexels-photo-358070.jpeg",
                    DownloadUrl = "https://example.com/games/racing-championship.zip",
                    ReleaseDate = DateTime.Now.AddMonths(-1),
                    Tags = new List<string> { "Racing", "Sports", "Multiplayer" }
                }
            };
        }

        public async Task<bool> DownloadGameAsync(Game game, IProgress<double> progress)
        {
            try
            {
                game.IsDownloading = true;
                game.Status = GameStatus.Installing;

                // Create game directory
                var gameDir = Path.Combine(_gamesDirectory, SanitizeFileName(game.Title));
                Directory.CreateDirectory(gameDir);

                // Simulate download progress
                for (int i = 0; i <= 100; i += 5)
                {
                    await Task.Delay(200); // Simulate download time
                    progress?.Report(i);
                    game.DownloadProgress = i;
                }

                // In a real implementation, you would:
                // 1. Download the actual game files from game.DownloadUrl
                // 2. Extract the files to the game directory
                // 3. Set up the executable path

                // For demo purposes, create a dummy executable
                var exePath = Path.Combine(gameDir, $"{SanitizeFileName(game.Title)}.exe");
                await File.WriteAllTextAsync(exePath, "Demo executable");

                game.InstallPath = gameDir;
                game.ExecutablePath = exePath;
                game.IsInstalled = true;
                game.IsDownloading = false;
                game.Status = GameStatus.Installed;

                await SaveGameConfigAsync(game);
                return true;
            }
            catch (Exception ex)
            {
                game.IsDownloading = false;
                game.Status = GameStatus.Error;
                // Log error
                return false;
            }
        }

        public async Task<bool> UpdateGameAsync(Game game, IProgress<double> progress)
        {
            try
            {
                game.IsUpdating = true;
                game.Status = GameStatus.Updating;

                // Simulate update progress
                for (int i = 0; i <= 100; i += 10)
                {
                    await Task.Delay(150);
                    progress?.Report(i);
                    game.DownloadProgress = i;
                }

                // In a real implementation, download and apply updates
                game.IsUpdating = false;
                game.Status = GameStatus.Installed;
                game.LastUpdated = DateTime.Now;

                await SaveGameConfigAsync(game);
                return true;
            }
            catch
            {
                game.IsUpdating = false;
                game.Status = GameStatus.Error;
                return false;
            }
        }

        public async Task<bool> LaunchGameAsync(Game game)
        {
            try
            {
                if (!game.IsInstalled || !File.Exists(game.ExecutablePath))
                    return false;

                game.Status = GameStatus.Running;

                // In a real implementation, launch the actual game executable
                // Process.Start(game.ExecutablePath);

                // For demo purposes, simulate game running
                await Task.Delay(2000);
                game.Status = GameStatus.Installed;

                return true;
            }
            catch
            {
                game.Status = GameStatus.Error;
                return false;
            }
        }

        public async Task<bool> UninstallGameAsync(Game game)
        {
            try
            {
                if (Directory.Exists(game.InstallPath))
                {
                    Directory.Delete(game.InstallPath, true);
                }

                game.IsInstalled = false;
                game.Status = GameStatus.NotInstalled;
                game.InstallPath = string.Empty;
                game.ExecutablePath = string.Empty;

                await RemoveGameConfigAsync(game);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DownloadDLCAsync(DLC dlc, IProgress<double> progress)
        {
            try
            {
                dlc.IsDownloading = true;

                // Simulate DLC download
                for (int i = 0; i <= 100; i += 8)
                {
                    await Task.Delay(100);
                    progress?.Report(i);
                    dlc.DownloadProgress = i;
                }

                dlc.IsInstalled = true;
                dlc.IsDownloading = false;

                return true;
            }
            catch
            {
                dlc.IsDownloading = false;
                return false;
            }
        }

        private async Task SaveGameConfigAsync(Game game)
        {
            try
            {
                var installedGames = await LoadInstalledGamesAsync();
                var existingGame = installedGames.Find(g => g.Id == game.Id);
                
                if (existingGame != null)
                {
                    installedGames.Remove(existingGame);
                }
                
                installedGames.Add(game);
                
                var json = JsonConvert.SerializeObject(installedGames, Formatting.Indented);
                await File.WriteAllTextAsync(_configPath, json);
            }
            catch
            {
                // Handle error
            }
        }

        private async Task RemoveGameConfigAsync(Game game)
        {
            try
            {
                var installedGames = await LoadInstalledGamesAsync();
                installedGames.RemoveAll(g => g.Id == game.Id);
                
                var json = JsonConvert.SerializeObject(installedGames, Formatting.Indented);
                await File.WriteAllTextAsync(_configPath, json);
            }
            catch
            {
                // Handle error
            }
        }

        public async Task<List<Game>> LoadInstalledGamesAsync()
        {
            try
            {
                if (!File.Exists(_configPath))
                    return new List<Game>();

                var json = await File.ReadAllTextAsync(_configPath);
                return JsonConvert.DeserializeObject<List<Game>>(json) ?? new List<Game>();
            }
            catch
            {
                return new List<Game>();
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}