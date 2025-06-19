using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameLauncher.Models
{
    public class Game : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private string _version = "1.0.0";
        private string _installPath = string.Empty;
        private string _executablePath = string.Empty;
        private string _imageUrl = string.Empty;
        private long _sizeInBytes;
        private bool _isInstalled;
        private bool _isDownloading;
        private bool _isUpdating;
        private double _downloadProgress;
        private GameStatus _status = GameStatus.NotInstalled;

        public int Id { get; set; }
        
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string InstallPath
        {
            get => _installPath;
            set => SetProperty(ref _installPath, value);
        }

        public string ExecutablePath
        {
            get => _executablePath;
            set => SetProperty(ref _executablePath, value);
        }

        public string ImageUrl
        {
            get => _imageUrl;
            set => SetProperty(ref _imageUrl, value);
        }

        public long SizeInBytes
        {
            get => _sizeInBytes;
            set => SetProperty(ref _sizeInBytes, value);
        }

        public bool IsInstalled
        {
            get => _isInstalled;
            set => SetProperty(ref _isInstalled, value);
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }

        public bool IsUpdating
        {
            get => _isUpdating;
            set => SetProperty(ref _isUpdating, value);
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        public GameStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string DownloadUrl { get; set; } = string.Empty;
        public string UpdateUrl { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<DLC> DLCs { get; set; } = new();

        public string FormattedSize => FormatBytes(SizeInBytes);

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public enum GameStatus
    {
        NotInstalled,
        Installing,
        Installed,
        Updating,
        UpdateAvailable,
        Running,
        Error
    }
}