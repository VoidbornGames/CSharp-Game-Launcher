using GameLauncher.Enums;
using GameLauncher.Models;
using GameLauncher.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GameLauncher.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly GameService _gameService;

        public ObservableCollection<Game> AllGames { get; set; } = new();
        public ObservableCollection<Game> FilteredGames { get; set; } = new();
        public ObservableCollection<Game> InstalledGames { get; } = new();
        public ObservableCollection<Game> StoreGames { get; } = new();

        private Game? _selectedGame;

        private PageType _currentPage = PageType.Library;
        public PageType CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public ICommand ShowLibraryCommand { get; }
        public ICommand ShowStoreCommand { get; }
        public ICommand ShowDownloadsCommand { get; }
        public ICommand ShowSettingsCommand { get; }

        public Game? SelectedGame
        {
            get => _selectedGame;
            set => SetProperty(ref _selectedGame, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterGames();
            }
        }

        public ICommand LaunchGameCommand { get; }
        public ICommand DownloadGameCommand { get; }
        public ICommand UpdateGameCommand { get; }
        public ICommand UninstallGameCommand { get; }
        public ICommand DownloadDLCCommand { get; }

        public MainViewModel()
        {
            _gameService = new GameService();

            LaunchGameCommand = new AsyncRelayCommand<Game>(LaunchGameAsync);
            DownloadGameCommand = new AsyncRelayCommand<Game>(DownloadGameAsync);
            UpdateGameCommand = new AsyncRelayCommand<Game>(UpdateGameAsync);
            UninstallGameCommand = new AsyncRelayCommand<Game>(UninstallGameAsync);
            DownloadDLCCommand = new AsyncRelayCommand<DLC>(DownloadDLCAsync);

            ShowLibraryCommand = new RelayCommand(() =>
            {
                CurrentPage = PageType.Library;
                FilterGames();
            });
            ShowStoreCommand = new RelayCommand(() =>
            {
                CurrentPage = PageType.Store;
                FilterGames();
            });
            ShowDownloadsCommand = new RelayCommand(() => CurrentPage = PageType.Downloads);
            ShowSettingsCommand = new RelayCommand(() => CurrentPage = PageType.Settings);

            _ = LoadGamesAsync();
        }

        private async Task LoadGamesAsync()
        {
            var games = await _gameService.GetAvailableGamesAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                AllGames.Clear();
                AllGames.Add(new Game
                {
                    Title = "Test Game",
                    Description = "A sample installed game",
                    Publisher = "Voidborn",
                    IsInstalled = true
                });
                foreach (var game in games)
                    AllGames.Add(game);
                FilterGames();
            });
        }

        private void FilterGames()
        {
            InstalledGames.Clear();
            StoreGames.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? AllGames
                : AllGames.Where(g => g.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var game in filtered)
            {
                if (game.IsInstalled)
                    InstalledGames.Add(game);
                else
                    StoreGames.Add(game);
            }
        }

        private async Task LaunchGameAsync(Game? game)
        {
            if (game == null)
                return;

            await _gameService.LaunchGameAsync(game);
        }

        private async Task DownloadGameAsync(Game? game)
        {
            if (game == null)
                return;

            var progress = new Progress<double>(p => game.DownloadProgress = p);
            await _gameService.DownloadGameAsync(game, progress);
        }

        private async Task UpdateGameAsync(Game? game)
        {
            if (game == null)
                return;

            var progress = new Progress<double>(p => game.DownloadProgress = p);
            await _gameService.UpdateGameAsync(game, progress);
        }

        private async Task UninstallGameAsync(Game? game)
        {
            if (game == null)
                return;

            if (MessageBox.Show("Are you sure you want to uninstall this game?",
                "Confirm Uninstall", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _gameService.UninstallGameAsync(game);
                game.IsInstalled = false;
                FilterGames();
            }
        }

        private async Task DownloadDLCAsync(DLC? dlc)
        {
            if (dlc == null || SelectedGame == null)
                return;

            var progress = new Progress<double>(p => dlc.DownloadProgress = p);
            await _gameService.DownloadDLCAsync(dlc, progress);
        }
    }
}
