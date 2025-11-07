using Flower.Core.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;


namespace Flower.App.ViewModels
{
    public interface ILoadFlowerViewModel
    {
        // Collection + selection used by the ListBox
        ObservableCollection<ShowFileItem> Shows { get; }
        ShowFileItem? SelectedShow { get; set; }

        // UI hints + state
        string FolderHint { get; }   // "Folder: …"
        bool CanLoad { get; }        // enables the Load button

        // Commands used by the buttons
        ReactiveCommand<Unit, Unit> RefreshCommand { get; }
        ReactiveCommand<Unit, Unit> BrowseFolderCommand { get; }
        ReactiveCommand<Unit, Unit> LoadCommand { get; }
        ReactiveCommand<Unit, Unit> CancelCommand { get; }

        // Lifecycle
        Task InitAsync(string? initialFolder = null);

        // Dialog close signal (not bound in XAML, but used by the window host)
        event EventHandler<ShowProject?>? CloseRequested;
    }
}
