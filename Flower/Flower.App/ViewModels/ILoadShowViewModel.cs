using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Flower.Core.Models;

namespace Flower.App.ViewModels
{
    public interface ILoadShowViewModel
    {
        // State
        ObservableCollection<ShowFileItem> Shows { get; }
        ShowFileItem? SelectedShow { get; set; }
        bool IsBusy { get; }
        string FolderHint { get; }
        bool CanLoad { get; }

        // Commands
        ReactiveCommand<Unit, Unit> RefreshCommand { get; }
        ReactiveCommand<Unit, Unit> BrowseFolderCommand { get; }
        ReactiveCommand<Unit, Unit> LoadCommand { get; }
        ReactiveCommand<Unit, Unit> CancelCommand { get; }

        // Lifecycle
        Task InitAsync(string? folder = null);

        // Close signal (result null = cancel)
        event EventHandler<ShowProject?>? CloseRequested;
    }
}
