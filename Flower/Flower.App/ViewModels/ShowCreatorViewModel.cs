// Flower.App/ViewModels/ShowCreatorViewModel.cs
using Flower.Core.Models;
using Flower.Core.Records;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace Flower.App.ViewModels;

public class ShowCreatorViewModel : ViewModelBase, IShowCreatorViewModel
{
    // ====== Properties ======

    private string _filePath = string.Empty;
    public string FilePath
    {
        get => _filePath;
        set => this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    private ShowProject _showProject = new();
    public ShowProject ShowProject
    {
        get => _showProject;
        set => this.RaiseAndSetIfChanged(ref _showProject, value);
    }

    // Backed by an ObservableCollection for easy binding,
    // but exposed as ICollection per your interface
    private ObservableCollection<ShowEvent> _availableShowEvents = new();
    public ICollection<ShowEvent> AvailableShowEvents
    {
        get => _availableShowEvents;
        set
        {
            // allow swapping the whole collection if you want
            if (value is ObservableCollection<ShowEvent> oc)
                this.RaiseAndSetIfChanged(ref _availableShowEvents, oc);
            else
            {
                _availableShowEvents = new ObservableCollection<ShowEvent>(value);
                this.RaisePropertyChanged(nameof(AvailableShowEvents));
            }
        }
    }

    // ====== Commands ======

    public ReactiveCommand<Unit, Unit> SaveShowProjectCommand { get; }
    public ReactiveCommand<Unit, Unit> EditShowProjectPropertiesCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviewShowCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadShowProjectCommand { get; }
    public ReactiveCommand<Unit, Unit> NewShowProjectCommand { get; }

    public ReactiveCommand<Unit, Unit> AddFlowerUnitCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveFlowerUnitCommand { get; }

    public ReactiveCommand<Unit, Unit> AddShowTrackCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveShowTrackCommand { get; }

    public ReactiveCommand<Unit, Unit> AddTrackEventCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveTrackEventCommand { get; }

    public ReactiveCommand<Unit, Unit> AddShowEvent { get; }
    public ReactiveCommand<Unit, Unit> RemoveShowEvent { get; }

    // ====== Ctor ======

    public ShowCreatorViewModel()
    {
        // Wire commands to stub async methods you can implement later.
        SaveShowProjectCommand = ReactiveCommand.CreateFromTask(SaveShowAsync);
        EditShowProjectPropertiesCommand = ReactiveCommand.CreateFromTask(EditProjectPropsAsync);
        PreviewShowCommand = ReactiveCommand.CreateFromTask(PreviewAsync);
        LoadShowProjectCommand = ReactiveCommand.CreateFromTask(LoadShowAsync);
        NewShowProjectCommand = ReactiveCommand.Create(NewShow);

        AddFlowerUnitCommand = ReactiveCommand.CreateFromTask(AddFlowerUnitAsync);
        RemoveFlowerUnitCommand = ReactiveCommand.CreateFromTask(RemoveFlowerUnitAsync);

        AddShowTrackCommand = ReactiveCommand.CreateFromTask(AddShowTrackAsync);
        RemoveShowTrackCommand = ReactiveCommand.CreateFromTask(RemoveShowTrackAsync);

        AddTrackEventCommand = ReactiveCommand.CreateFromTask(AddTrackEventAsync);
        RemoveTrackEventCommand = ReactiveCommand.CreateFromTask(RemoveTrackEventAsync);

        AddShowEvent = ReactiveCommand.CreateFromTask(AddShowEventAsync);
        RemoveShowEvent = ReactiveCommand.CreateFromTask(RemoveShowEventAsync);
    }

    // ====== Stubs (fill these in your way) ======

    private Task SaveShowAsync()
    {
        // TODO: serialize ShowProject to FilePath (use your ShowStore if you like)
        return Task.CompletedTask;
    }

    private Task EditProjectPropsAsync()
    {
        // TODO: open a dialog to edit project-level properties (Title, Repeat, etc.)
        return Task.CompletedTask;
    }

    private Task PreviewAsync()
    {
        // TODO: call your ShowPlayer.PlayAsync(ShowProject) via an injected service
        return Task.CompletedTask;
    }

    private Task LoadShowAsync()
    {
        // TODO: open file dialog or load from FilePath; assign to ShowProject
        // then refresh AvailableShowEvents if you keep templates here
        return Task.CompletedTask;
    }

    private void NewShow()
    {
        // TODO: create empty project (flowers/tracks as you prefer)
        ShowProject = new ShowProject();
        _availableShowEvents.Clear();
    }

    private Task AddFlowerUnitAsync()
    {
        // TODO: show dialog, add a FlowerUnit to ShowProject.Flowers
        return Task.CompletedTask;
    }

    private Task RemoveFlowerUnitAsync()
    {
        // TODO: remove currently selected FlowerUnit (you may add a SelectedFlower property)
        return Task.CompletedTask;
    }

    private Task AddShowTrackAsync()
    {
        // TODO: show dialog to name a track + loop ms, then add to ShowProject.Tracks
        return Task.CompletedTask;
    }

    private Task RemoveShowTrackAsync()
    {
        // TODO: remove selected track (consider adding SelectedTrack to this VM)
        return Task.CompletedTask;
    }

    private Task AddTrackEventAsync()
    {
        // TODO: show dialog to pick a ShowEvent template + AtMs; push TrackEvent to selected track
        return Task.CompletedTask;
    }

    private Task RemoveTrackEventAsync()
    {
        // TODO: remove selected TrackEvent (consider adding SelectedTrackEvent to this VM)
        return Task.CompletedTask;
    }

    private Task AddShowEventAsync()
    {
        // TODO: show dialog to define a reusable ShowEvent (CmdId + Args),
        // then add it to AvailableShowEvents
        return Task.CompletedTask;
    }

    private Task RemoveShowEventAsync()
    {
        // TODO: remove selected ShowEvent from AvailableShowEvents
        return Task.CompletedTask;
    }
}
