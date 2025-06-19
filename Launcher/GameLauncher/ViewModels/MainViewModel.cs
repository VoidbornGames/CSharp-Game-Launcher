using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using GameLauncher.Models;
using GameLauncher.Services;
using Microsoft.Toolkit.Mvvm.Input;

namespace GameLauncher.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly GameService _gameService;
        private string _searchText = string.Empty;
        private Game? _selectedGame;
        private bool _isLoading;

        public MainViewModel()
        {
            _gameService = new GameService();
            Games = new ObservableCollection<Game>();
            FilteredGames = new ObservableCollection<Game>();
            
            LoadGamesCommand = new AsyncRelayCommand(LoadGamesAsync);
            DownloadGameCommand = new AsyncRelayCommand<Game>(DownloadGameAsync);
            UpdateGameCommand = new AsyncRelayCommand<Game>(UpdateGameAsync);
            LaunchGameCommand = new AsyncRelayCommand<Game>(LaunchGameAsync);
            UninstallGameCommand = new AsyncRelayCommand<Game>(UninstallGameAsync);
            DownloadDLCCommand = new AsyncRelayCommand<DLC>(DownloadDLCAsync);
            SearchCommand = new RelayCommand(FilterGames);

            _ = LoadGamesAsync();
        }

        public ObservableCollection<Game> Games { get; }
        public ObservableCollection<Game> FilteredGames { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterGames();
            }
        }

        public Game? SelectedGame
        {
            get => _selectedGame;
            set => SetProperty(ref _selectedGame, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoadGamesCommand { get; }
        public ICommand DownloadGameCommand { get; }
        public ICommand UpdateGameCommand { get; }
        public ICommand LaunchGameCommand { get; }
        public ICommand UninstallGameCommand { get; }
        public ICommand DownloadDLCCommand { get; }
        public ICommand SearchCommand { get; }

        private async Task LoadGamesAsync()
        {
            try
            {
                IsLoading = true;
                
                var availableGames = await _gameService.GetAvailableGamesAsync();
                var installedGames = await _gameService.LoadInstalledGamesAsync();

                Games.Clear();
                
                foreach (var game in availableGames)
                {
                    var installedGame = installedGames.FirstOrDefault(g => g.Id == game.Id);
                    if (installedGame != null)
                    {
                        game.IsInstalled = installedGame.IsInstalled;
                        game.InstallPath = installedGame.InstallPath;
                        game.ExecutablePath = installedGame.ExecutablePath;
                        game.Status = installedGame.Status;
                    }
                    
                    Games.Add(game);
                }

                FilterGames();
            }
            catch (Exception ex)
            {
                // Handle error - in a real app, show error message to user
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DownloadGameAsync(Game? game)
        {
            if (game == null) return;

            var progress = new Progress<double>(value =>
            {
                game.DownloadProgress = value;
            });

            await _gameService.DownloadGameAsync(game, progress);
        }

        private async Task UpdateGameAsync(Game? game)
        {
            if (game == null) return;

            var progress = new Progress<double>(value =>
            {
                game.DownloadProgress = value;
            });

            await _gameService.UpdateGameAsync(game, progress);
        }

        private async Task LaunchGameAsync(Game? game)
        {
            if (game == null) return;
            await _gameService.LaunchGameAsync(game);
        }

        private async Task UninstallGameAsync(Game? game)
        {
            if (game == null) return;
            await _gameService.UninstallGameAsync(game);
        }

        private async Task DownloadDLCAsync(DLC? dlc)
        {
            if (dlc == null) return;

            var progress = new Progress<double>(value =>
            {
                dlc.DownloadProgress = value;
            });

            await _gameService.DownloadDLCAsync(dlc, progress);
        }

        private void FilterGames()
        {
            FilteredGames.Clear();
            
            var filtered = string.IsNullOrWhiteSpace(SearchText) 
                ? Games 
                : Games.Where(g => g.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                  g.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                  g.Tags.Any(t => t.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

            foreach (var game in filtered)
            {
                FilteredGames.Add(game);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}