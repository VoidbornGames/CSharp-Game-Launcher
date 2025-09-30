using GameLauncher.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GameLauncher.Services
{
    public class GameService
    {
        private readonly HttpClient _httpClient;
        private readonly string _gamesDirectory;
        private readonly string _userDataPath;
        private readonly string _configPath;
        private readonly string _rootPath;
        private readonly string _version = "1.0.0";

        public GameService()
        {
            _httpClient = new HttpClient();
            _gamesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Games");
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.data");
            _rootPath = Path.GetPathRoot(_gamesDirectory);
            _userDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user.data");


            Directory.CreateDirectory(_gamesDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);

            AfterInit();
        }

        public async void AfterInit()
        {
            otw otw = new otw();
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "otw.data")))
            {
                otw = JsonConvert.DeserializeObject<otw>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "otw.data")));
                try
                {
                    if(await IsInternetAvailableAsync())
                    {
                        TrackDownload("First Use");
                    }
                }
                catch { }
            }

            if (otw.haveBeenShown == false)
            {
                otw.haveBeenShown = true;
                SendLicenseMessage(otw.haveBeenShown, _version);
            }

            if (await IsInternetAvailableAsync())
                _ = createUserData();
        }

        public async Task<List<Game>> GetAvailableGamesAsync()
        {
            // Check for launcher new updates
            /*(await IsInternetAvailableAsync())
            {
                string versionURL = @"https://drive.usercontent.google.com/download?id=1Nqk4XcwLiXkscQTpOb62O0hr8mtr1f5g&export=download";

                // Geting the launcher's online version
                string version = await _httpClient.GetStringAsync(versionURL);

                otw otw = new otw();
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "otw.data")))
                {
                    otw = JsonConvert.DeserializeObject<otw>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "otw.data")));
                    if (otw.version != _version)
                        MessageBox.Show("There is a new version available! do you want to install it?"
                            , "New Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
                }
            }*/

            // In a real implementation, this would fetch from a server
            // For demo purposes, return sample games
            if(await IsInternetAvailableAsync())
            {
                string configURL = @"https://drive.usercontent.google.com/download?id=1Nqk4XcwLiXkscQTpOb62O0hr8mtr1f5g&export=download";

                // Geting the config data
                string data = await _httpClient.GetStringAsync(configURL);

                // Checking if the data is valid
                if (data != null)
                {
                    var _data = JsonConvert.DeserializeObject<List<Game>>(data);
                    if (_data != null)
                        return _data;
                    else
                        return await LoadInstalledGamesAsync();
                }
                else
                    return await LoadInstalledGamesAsync();
            }
            else
                return await LoadInstalledGamesAsync();
        }

        public async Task<bool> DownloadGameAsync(Game game, IProgress<double> progress)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (await IsDriveDontHaveEnoughSpace(_rootPath, game, drive))
                {
                    game.IsDownloading = false;
                    game.Status = GameStatus.Error;

                    MessageBox.Show("You need at least " + (game.SizeInBytes * 2.3) / 1048576 + "MB free space",
                        "Not enough space!", MessageBoxButton.OK, MessageBoxImage.Warning);

                    return false;
                }
            }

            try
            {
                game.IsDownloading = true;
                game.Status = GameStatus.Installing;

                // Create game directory
                var gameDir = Path.Combine(_gamesDirectory, SanitizeFileName(game.Title));
                Directory.CreateDirectory(gameDir);

                // Temporary zip file path
                var zipFilePath = Path.Combine(gameDir, $"{SanitizeFileName(game.Title)}.zip");

                // Download zip file with progress
                using (var response = await _httpClient.GetAsync(game.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1 && progress != null;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int read;
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            if (canReportProgress)
                            {
                                double percent = (double)totalRead / totalBytes * 100;
                                progress.Report(percent);
                                game.DownloadProgress = percent;
                            }
                        }
                    }
                }

                // Extract the zip file
                ZipFile.ExtractToDirectory(zipFilePath, gameDir);

                // Delete the zip file
                File.Delete(zipFilePath);

                // Find the main executable (you can improve this to detect better)
                var exeFiles = Directory.GetFiles(gameDir, "*.exe", SearchOption.AllDirectories);
                var mainExe = exeFiles.Length > 0 ? exeFiles[0] : null;

                game.InstallPath = gameDir;
                game.ExecutablePath = mainExe ?? "";
                game.IsInstalled = true;
                game.IsDownloading = false;
                game.Status = GameStatus.Installed;

                await SaveGameConfigAsync(game);
                TrackDownload(game.Title);

                return true;
            }
            catch (Exception ex)
            {
                game.IsDownloading = false;
                game.Status = GameStatus.Error;

                MessageBox.Show(ex.Message, "Something went wrong!",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
        }

        public async Task<bool> UpdateGameAsync(Game game, IProgress<double> progress)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (await IsDriveDontHaveEnoughSpace(_rootPath, game, drive))
                {
                    game.IsDownloading = false;
                    game.Status = GameStatus.Error;

                    MessageBox.Show("You need at least " + (game.SizeInBytes * 2.3) / 1048576 + "MB free space",
                        "Not enough space!", MessageBoxButton.OK, MessageBoxImage.Warning);

                    return false;
                }
            }

            try
            {
                game.IsUpdating = true;
                game.Status = GameStatus.Updating;

                var gameDir = Path.Combine(_gamesDirectory, SanitizeFileName(game.Title));
                Directory.CreateDirectory(gameDir);

                var zipFilePath = Path.Combine(gameDir, $"{SanitizeFileName(game.Title)}_update.zip");

                // Download zip file with progress
                using (var response = await _httpClient.GetAsync(game.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1 && progress != null;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int read;
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            if (canReportProgress)
                            {
                                double percent = (double)totalRead / totalBytes * 100;
                                progress.Report(percent);
                                game.DownloadProgress = percent;
                            }
                        }
                    }
                }

                // Extract and overwrite files
                ZipFile.ExtractToDirectory(zipFilePath, gameDir, overwriteFiles: true);

                // Delete zip
                File.Delete(zipFilePath);

                // Optionally update executable path if needed
                var exeFiles = Directory.GetFiles(gameDir, "*.exe", SearchOption.AllDirectories);
                game.ExecutablePath = exeFiles.Length > 0 ? exeFiles[0] : game.ExecutablePath;

                game.IsUpdating = false;
                game.Status = GameStatus.Installed;
                game.LastUpdated = DateTime.Now;

                await SaveGameConfigAsync(game);
                return true;
            }
            catch (Exception ex)
            {
                game.IsUpdating = false;
                game.Status = GameStatus.Error;

                MessageBox.Show(ex.Message, "Update failed!", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Process.Start(game.ExecutablePath);

                // For demo purposes, simulate game running
                await Task.Delay(2000);
                game.Status = GameStatus.Installed;

                return true;
            }
            catch(Exception ex)
            {
                game.Status = GameStatus.Error;

                MessageBox.Show(ex.Message, "Something went wrong!",
                    MessageBoxButton.OK, MessageBoxImage.Error);

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
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Something went wrong!",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
        }

        public async Task<bool> DownloadDLCAsync(DLC dlc, IProgress<double> progress)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.Name == _rootPath && drive.AvailableFreeSpace < (dlc.SizeInBytes * 2.3))
                {
                    dlc.IsDownloading = false;

                    MessageBox.Show("You need at least " + (dlc.SizeInBytes * 2.3) / 1048576 + "MB free space",
                        "Not enough space!", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Log error
                    return false;
                }
            }

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
            catch(Exception ex)
            {
                dlc.IsDownloading = false;

                MessageBox.Show(ex.Message, "Something went wrong!",
                    MessageBoxButton.OK, MessageBoxImage.Error);

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
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Something went wrong!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Something went wrong!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<List<Game>> LoadInstalledGamesAsync()
        {
            try
            {
                if (!File.Exists(_configPath))
                    return new List<Game>();

                var json = await File.ReadAllTextAsync(_configPath);
                var gameData = JsonConvert.DeserializeObject<List<Game>>(json) ?? new List<Game>();

                return gameData;
            }
            catch
            {
                return new List<Game>();
            }
        }

        private async Task createUserData()
        {
            if (!File.Exists(_userDataPath))
            {
                var uuid = JsonConvert.DeserializeObject<Guid>(await SendRequest("5.57.34.86", 11001, "makeUUID", ""));

                userData jsonData = new()
                {
                    Username = "user",
                    Password = "ali2020",
                    uuid = uuid,
                    Role = "player"
                };
                var userJson = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
                await File.WriteAllTextAsync(_userDataPath, userJson);

                await SendRequest("5.57.34.86", 11001, "createUser", userJson);
            }
        }

        public async Task UpdateUserName(string gameTitle, string newName)
        {
            var userDataPath = Path.Combine(_gamesDirectory, SanitizeFileName(gameTitle), "user.data");
            if (File.Exists(userDataPath))
            {
                // Load existing
                var json = await File.ReadAllTextAsync(userDataPath);
                var data = JsonConvert.DeserializeObject<userData>(json);

                // Update
                data.Username = newName;

                // Save back
                var updatedJson = JsonConvert.SerializeObject(data, Formatting.Indented);
                await File.WriteAllTextAsync(userDataPath, updatedJson);
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

        public static async Task<bool> IsInternetAvailableAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3); // Don't wait too long
                                                          // Use a lightweight, reliable endpoint (e.g., Google)
                var response = await client.GetAsync("http://www.google.com/generate_204");
                return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> IsDriveDontHaveEnoughSpace(string _rootPath, Game game, DriveInfo drive)
        {
            try
            {
                if (drive.Name == _rootPath && drive.AvailableFreeSpace < (game.SizeInBytes * 2.3))
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static void SendLicenseMessage(bool haveBeenShown, string version)
        {
            otw otw = new otw();

            otw.haveBeenShown = haveBeenShown;
            otw.version = version;

            string json = JsonConvert.SerializeObject(otw);
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "otw.data"), json);

            MessageBox.Show
                ("Were not responsible for any game that is not published by Voidborn Games Take it at your own risk.",
                "Please Be Aware!",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public async void TrackDownload(string gameName)
        {
            string url = "https://script.google.com/macros/s/AKfycbzAOPX7lUT8roa8FktiXZbe_fBT6X10WTK5SCtBhDM11FyPys_oWkGjnqc7yBZIO76m/exec";
            string requestUrl = $"{url}?game_name={Uri.EscapeDataString(gameName)}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    await client.GetAsync(requestUrl);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static async Task<string> SendRequest(string serverIp, int port, string request, string inputData)
        {
            var data = new Data { theCommand = request, jsonData = inputData };

            try
            {
                using TcpClient client = new TcpClient();
                await client.ConnectAsync(serverIp, port);

                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                using var reader = new StreamReader(stream, Encoding.UTF8);

                // --- Send JSON with newline (important for server's ReadLineAsync) ---
                string json = JsonConvert.SerializeObject(data);
                await writer.WriteLineAsync(json);

                // --- Receive response ---
                string? responseJson = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(responseJson))
                {
                    var response = JsonConvert.DeserializeObject<Data>(responseJson);
                    if (response != null)
                        return response.jsonData;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }

            return string.Empty;
        }

        public class Data
        {
            public string theCommand { get; set; }
            public string jsonData { get; set; }
        }

        struct otw
        {
            public bool haveBeenShown;
            public string version;
        }

        public class userData
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public Guid uuid { get; set; }
            public string Role { get; set; } = "player";
        }
    }
}
