using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Flower.Core.Abstractions.Stores;
using Flower.Core.Models;
using Flower.Infrastructure.Persistence;
using ReactiveUI;

namespace Flower.App.ViewModels
{
    public sealed class LoadShowViewModel : ViewModelBase, ILoadShowViewModel
    {
        private readonly IShowProjectStore _showStore;
        private readonly Window? _owner;

        private string _folder = ShowProjectStore.DefaultFolder;
        private ShowFileItem? _selected;
        private bool _isBusy;

        public ObservableCollection<ShowFileItem> Shows { get; } = new();

        public ShowFileItem? SelectedShow
        {
            get => _selected;
            set
            {
                this.RaiseAndSetIfChanged(ref _selected, value);
                this.RaisePropertyChanged(nameof(CanLoad));
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                this.RaiseAndSetIfChanged(ref _isBusy, value);
                this.RaisePropertyChanged(nameof(CanLoad));
            }
        }

        public string FolderHint => $"Folder: {_folder}";
        public bool CanLoad => SelectedShow is not null && !IsBusy;

        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public event EventHandler<ShowProject?>? CloseRequested;

        public LoadShowViewModel(IShowProjectStore showStore, Window? owner = null)
        {
            _showStore = showStore ?? throw new ArgumentNullException(nameof(showStore));
            _owner = owner;

            RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
            BrowseFolderCommand = ReactiveCommand.CreateFromTask(BrowseFolderAsync);

            var canLoadObs = this.WhenAnyValue(x => x.SelectedShow, x => x.IsBusy,
                (s, b) => s is not null && !b);

            LoadCommand = ReactiveCommand.CreateFromTask(LoadAsync, canLoadObs);
            CancelCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke(this, null));

            // Diagnostics (keeps ReactiveUI from hard-crashing)
            LoadCommand.ThrownExceptions.Subscribe(ex => Console.WriteLine("[Load] " + ex));
            RefreshCommand.ThrownExceptions.Subscribe(ex => Console.WriteLine("[Refresh] " + ex));
            BrowseFolderCommand.ThrownExceptions.Subscribe(ex => Console.WriteLine("[Browse] " + ex));
        }


        public async Task InitAsync(string? folder = null)
        {
            if (!string.IsNullOrWhiteSpace(folder))
                _folder = folder!;
            await RefreshAsync();
            this.RaisePropertyChanged(nameof(FolderHint));
        }

        private async Task RefreshAsync()
        {
            IsBusy = true;
            try
            {
                Shows.Clear();
                if (!Directory.Exists(_folder)) return;

                var files = Directory.EnumerateFiles(_folder, "*.json")
                                     .OrderBy(Path.GetFileName);
                foreach (var f in files)
                    Shows.Add(new ShowFileItem(f));

                if (SelectedShow is null)
                    SelectedShow = Shows.FirstOrDefault();
            }
            finally
            {
                IsBusy = false;
                this.RaisePropertyChanged(nameof(FolderHint));
            }
        }

        private async Task BrowseFolderAsync()
        {
            if (_owner?.StorageProvider is IStorageProvider sp)
            {
                var folders = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
                if (folders is { Count: > 0 })
                {
                    _folder = folders[0].Path.LocalPath;
                    await RefreshAsync();
                }
            }
        }

        private async Task LoadAsync()
        {
            if (_showStore is null) // should be impossible after ctor change
            {
                Console.WriteLine("[LoadAsync] _showStore is null");
                CloseRequested?.Invoke(this, null);
                return;
            }

            var path = SelectedShow?.Path;
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("[LoadAsync] SelectedShow or Path is null/empty");
                CloseRequested?.Invoke(this, null);
                return;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine($"[LoadAsync] File does not exist: {path}");
                CloseRequested?.Invoke(this, null);
                return;
            }

            IsBusy = true;
            try
            {
                var project = await _showStore.LoadAsync(path).ConfigureAwait(false);
                CloseRequested?.Invoke(this, project);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[LoadAsync] Exception: " + ex);
                CloseRequested?.Invoke(this, null);
            }
            finally
            {
                IsBusy = false;
            }
        }

    }
}
