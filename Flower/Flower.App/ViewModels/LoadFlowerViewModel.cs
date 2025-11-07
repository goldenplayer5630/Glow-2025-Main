using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Flower.Core.Abstractions.Stores;
using Flower.Core.Models;
using Flower.Infrastructure.Persistence;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public sealed class LoadFlowerViewModel : ViewModelBase, ILoadFlowerViewModel
    {
        private readonly IShowProjectStore _showStore;
        private readonly Window? _hostWindow;

        private string _folder = ShowProjectStore.DefaultFolder;

        private ShowFileItem? _selectedShow;
        public ShowFileItem? SelectedShow
        {
            get => _selectedShow;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedShow, value);
                this.RaisePropertyChanged(nameof(CanLoad)); // recompute
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                this.RaiseAndSetIfChanged(ref _isBusy, value);
                this.RaisePropertyChanged(nameof(CanLoad)); // recompute
            }
        }

        public ObservableCollection<ShowFileItem> Shows { get; } = new();

        public string FolderHint => $"Folder: {_folder}";
        public bool CanLoad => SelectedShow is not null && !IsBusy;

        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public event EventHandler<ShowProject?>? CloseRequested;

        public LoadFlowerViewModel(IShowProjectStore showStore, Window? hostWindow = null)
        {
            _showStore = showStore;
            _hostWindow = hostWindow;

            RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
            BrowseFolderCommand = ReactiveCommand.CreateFromTask(BrowseFolderAsync);

            // Drive can-execute directly from SelectedShow + IsBusy
            var canLoadObs = this
                .WhenAnyValue(vm => vm.SelectedShow, vm => vm.IsBusy,
                              (sel, busy) => sel is not null && !busy);

            LoadCommand = ReactiveCommand.CreateFromTask(LoadAsync, canLoadObs);

            CancelCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke(this, null));
        }

        public async Task InitAsync(string? initialFolder = null)
        {
            if (!string.IsNullOrWhiteSpace(initialFolder))
                _folder = initialFolder!;
            await RefreshAsync();
            Console.WriteLine($"[LoadFlowerVM] Folder={_folder}");
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

                // Optionally preselect first item
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
            if (_hostWindow?.StorageProvider is IStorageProvider sp)
            {
                var folder = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    AllowMultiple = false
                });
                if (folder is { Count: > 0 })
                {
                    _folder = folder[0].Path.LocalPath;
                    await RefreshAsync();
                }
            }
            // else: expose a textbox in UI if you want manual path entry
        }

        private async Task LoadAsync()
        {
            if (SelectedShow is null) return;

            IsBusy = true;
            try
            {
                var project = await _showStore.LoadAsync(SelectedShow.Path).ConfigureAwait(false);
                CloseRequested?.Invoke(this, project);
            }
            catch
            {
                CloseRequested?.Invoke(this, null);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
