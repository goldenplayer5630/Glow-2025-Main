// Flower.App/ViewModels/AppViewModel.cs
using Avalonia.Threading;
using Flower.App.Views;
using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Abstractions.Stores;
using Flower.Core.Enums;
using Flower.Core.Models;
using Flower.Core.Records;
using Flower.Core.Services;
using ReactiveUI;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Flower.App.ViewModels;

public sealed class AppViewModel : ViewModelBase, IAppViewModel, IDisposable
{
    // ========= Fields =========
    private readonly IBusDirectory _busDirectory;        // ⬅️ multi-bus manager
    private readonly IShowPlayerService _player;
    private readonly IShowProjectStore _showStore;
    private readonly IFlowerService _flowerService;

    private ShowProject? _project;

    // Subscriptions (fleet + selection)
    private IDisposable? _flowersChangedSub;
    private readonly List<IDisposable> _itemStatusSubs = new();
    private IDisposable? _selectedFlowerSub;
    private readonly ObservableAsPropertyHelper<bool> _hasFlowers;
    private readonly ObservableAsPropertyHelper<bool> _anySelectedForAssign;

    private bool _disposed;

    // ========= Bindable State =========
    public ObservableCollection<string> Ports { get; } = new();

    private object? _currentPage;
    public object? CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    // Two bus selectors instead of one
    private string? _selectedPortBus0;
    public string? SelectedPortBus0
    {
        get => _selectedPortBus0;
        set => this.RaiseAndSetIfChanged(ref _selectedPortBus0, value);
    }

    private string? _selectedPortBus1;
    public string? SelectedPortBus1
    {
        get => _selectedPortBus1;
        set => this.RaiseAndSetIfChanged(ref _selectedPortBus1, value);
    }

    private string _statusText = "Select ports and click Connect.";
    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    private string _monitorText = "";
    public string MonitorText
    {
        get => _monitorText;
        set => this.RaiseAndSetIfChanged(ref _monitorText, value);
    }

    // ========= Flowers (data + selection) =========
    public ReadOnlyObservableCollection<FlowerUnit> Flowers => _flowerService.Flowers;

    private FlowerUnit? _selectedFlower;
    public FlowerUnit? SelectedFlower
    {
        get => _selectedFlower;
        set
        {
            if (Equals(_selectedFlower, value)) return;

            this.RaiseAndSetIfChanged(ref _selectedFlower, value);

            _selectedFlowerSub?.Dispose();
            _selectedFlowerSub = null;

            if (value is INotifyPropertyChanged inpc)
            {
                _selectedFlowerSub =
                    Observable.FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                            h => (s, e) => h(e),
                            h => inpc.PropertyChanged += h,
                            h => inpc.PropertyChanged -= h)
                        .Where(e => string.IsNullOrEmpty(e.PropertyName) ||
                                    e.PropertyName == nameof(FlowerUnit.ConnectionStatus))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ => RaiseConnectStatesChanged());
            }

            RaiseConnectStatesChanged();
            this.RaisePropertyChanged(nameof(SelectedFlowerInfo));
        }
    }

    public string SelectedFlowerInfo =>
        SelectedFlower is null
            ? "No flower selected."
            : $"Selected: Id={SelectedFlower.Id}, Category={SelectedFlower.Category}, Status={SelectedFlower.ConnectionStatus}, Brightness={SelectedFlower.CurrentBrightness}";

    // ========= Visibility Helpers: Single Flower =========
    public bool CanConnect => SelectedFlower?.ConnectionStatus == ConnectionStatus.Disconnected;
    public bool CanDisconnect => SelectedFlower?.ConnectionStatus == ConnectionStatus.Connected;
    public bool CanReconnect => SelectedFlower?.ConnectionStatus == ConnectionStatus.Degraded;
    public bool DisabledConnect => !CanConnect && !CanDisconnect && !CanReconnect;

    // ========= Visibility Helpers: Fleet =========
    public bool HasFlowers => _hasFlowers.Value;
    public bool CanConnectAll => HasFlowers && Flowers.Any(f => f.ConnectionStatus != ConnectionStatus.Connected);
    public bool CanDisconnectAll => HasFlowers && Flowers.All(f => f.ConnectionStatus == ConnectionStatus.Connected);
    public bool DisabledConnectAll => !HasFlowers || (!CanConnectAll && !CanDisconnectAll);
    public bool CanAssignBuses => _anySelectedForAssign.Value;

    // ========= Commands =========
    public ReactiveCommand<Unit, Unit> LoadShowCommand { get; }
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    public ReactiveCommand<Unit, Unit> ConnectFlowerCommand { get; }
    public ReactiveCommand<Unit, Unit> DisconnectFlowerCommand { get; }
    public ReactiveCommand<Unit, Unit> ReconnectFlowerCommand { get; }

    public ReactiveCommand<Unit, Unit> ConnectAllFlowersCommand { get; }
    public ReactiveCommand<Unit, Unit> DisconnectAllFlowersCommand { get; }
    public ReactiveCommand<Unit, Unit> AssignBusesCommand { get; }

    public ReactiveCommand<Unit, Unit> AddFlowerCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateFlowerCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedFlowerCommand { get; }

    // ========= Dialog Interactions =========
    public ReactiveCommand<Unit, Unit> OpenShowCreatorCommand { get; }
    public Interaction<Unit, Unit> OpenShowCreatorInteraction { get; } = new();
    public ReactiveCommand<Unit, Unit> OpenManageBusesCommand { get; }
    public Interaction<Unit, Unit> OpenManageBusesInteraction { get; } = new();

    public Interaction<Unit, FlowerUnit?> AddFlowerInteraction { get; } = new();
    public Interaction<FlowerUnit, FlowerUnit?> UpdateFlowerInteraction { get; } = new();
    public Interaction<IReadOnlyList<FlowerUnit>, string?> AssignBusesInteraction { get; } = new();

    // ========= Ctor =========
    public AppViewModel(
        IBusDirectory busDirectory,          // ⬅️ injected instead of ITransport
        IShowPlayerService player,
        IShowProjectStore showStore,
        IFlowerService flowerService)
    {
        _busDirectory = busDirectory;
        _player = player;
        _showStore = showStore;
        _flowerService = flowerService;

        // HasFlowers observable
        var hasFlowersObs =
            Observable.Merge(
                Observable.Return(Flowers.Count),
                Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                        h => ((INotifyCollectionChanged)Flowers).CollectionChanged += h,
                        h => ((INotifyCollectionChanged)Flowers).CollectionChanged -= h)
                    .Select(_ => Flowers.Count))
            .Select(count => count > 0)
            .ObserveOn(RxApp.MainThreadScheduler);

        _hasFlowers = hasFlowersObs.ToProperty(this, vm => vm.HasFlowers, scheduler: RxApp.MainThreadScheduler);

        // After _hasFlowers setup, wire "any selected" observable
        var anySelectedObs =
            Observable.Merge(
                // seed
                Observable.Return(Flowers.Any(f => f.AssignSelected)),
                // when collection changes -> resubscribe row-level changes
                Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                        h => ((INotifyCollectionChanged)Flowers).CollectionChanged += h,
                        h => ((INotifyCollectionChanged)Flowers).CollectionChanged -= h)
                    .Select(_ => true)
                    .StartWith(true)
                    .Select(_ =>
                    {
                        // subscribe to each row's AssignSelected change
                        foreach (var d in _itemStatusSubs) d.Dispose();
                        _itemStatusSubs.Clear();

                        foreach (var f in Flowers)
                        {
                            if (f is INotifyPropertyChanged inpc)
                            {
                                var sub =
                                    Observable.FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                                            h => (s, e) => h(e),
                                            h => inpc.PropertyChanged += h,
                                            h => inpc.PropertyChanged -= h)
                                        .Where(e => e.PropertyName == nameof(FlowerUnit.AssignSelected))
                                        .ObserveOn(RxApp.MainThreadScheduler)
                                        .Subscribe(__ => this.RaisePropertyChanged(nameof(CanAssignBuses)));

                                _itemStatusSubs.Add(sub);
                            }
                        }

                        return Flowers.Any(ff => ff.AssignSelected);
                    })
            )
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler);

        _anySelectedForAssign = anySelectedObs.ToProperty(this, vm => vm.CanAssignBuses, scheduler: RxApp.MainThreadScheduler);

        // Command
        AssignBusesCommand = ReactiveCommand.CreateFromTask(AssignBusesAsync, anySelectedObs);

        // Commands
        ConnectFlowerCommand = ReactiveCommand.CreateFromTask(ConnectFlowerAsync, this.WhenAnyValue(vm => vm.SelectedFlower).Select(sf => sf != null));
        DisconnectFlowerCommand = ReactiveCommand.CreateFromTask(DisconnectFlowerAsync, this.WhenAnyValue(vm => vm.SelectedFlower).Select(sf => sf != null));
        ReconnectFlowerCommand = ReactiveCommand.CreateFromTask(ReconnectFlowerAsync, this.WhenAnyValue(vm => vm.SelectedFlower).Select(sf => sf != null));

        ConnectAllFlowersCommand = ReactiveCommand.CreateFromTask(ConnectAllFlowersAsync, this.WhenAnyValue(vm => vm.HasFlowers));
        DisconnectAllFlowersCommand = ReactiveCommand.CreateFromTask(DisconnectAllFlowersAsync, this.WhenAnyValue(vm => vm.HasFlowers));

        LoadShowCommand = ReactiveCommand.CreateFromTask(LoadShowAsync);
        PlayCommand = ReactiveCommand.CreateFromTask(PlayAsync);
        StopCommand = ReactiveCommand.Create(Stop);

        OpenShowCreatorCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await OpenShowCreatorInteraction.Handle(Unit.Default);
        });

        OpenManageBusesCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await OpenManageBusesInteraction.Handle(Unit.Default);
        });

        AddFlowerCommand = ReactiveCommand.CreateFromTask(AddFlowerAsync);
        UpdateFlowerCommand = ReactiveCommand.CreateFromTask(
            UpdateFlowerAsync,
            this.WhenAnyValue(vm => vm.SelectedFlower).Select(sf => sf != null));
        DeleteSelectedFlowerCommand = ReactiveCommand.CreateFromTask(
            DeleteSelectedFlowerAsync,
            this.WhenAnyValue(vm => vm.SelectedFlower).Select(sf => sf != null));

        // Keep info text in sync on selection
        this.WhenAnyValue(vm => vm.SelectedFlower)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(SelectedFlowerInfo)));

        this.WhenAnyValue(vm => vm.HasFlowers)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => RaiseFleetStatesChanged());

        RefreshPorts();
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            RefreshPorts();
        });
        _ = LoadFlowersAsync(); // initial load
    }

    // ========= Serial (multi-bus) & Show Actions =========
    private void RefreshPorts()
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // RJCP (preferred)
        try
        {
            // FIX: SerialPortStream.GetPortNames() is an instance method, not static.
            using (var sps = new SerialPortStream())
            {
                foreach (var p in sps.GetPortNames())
                    if (!string.IsNullOrWhiteSpace(p)) found.Add(p.Trim());
            }
        }
        catch { /* ignore */ }

        // Fallback to BCL
        try
        {
            foreach (var p in System.IO.Ports.SerialPort.GetPortNames())
                if (!string.IsNullOrWhiteSpace(p)) found.Add(p.Trim());
        }
        catch { /* ignore */ }

        // Sort nicely (COM1, COM2, ..., COM10)
        static int PortOrder(string name)
        {
            if (name.StartsWith("COM", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(name.Substring(3), out var n)) return n;
            return int.MaxValue;
        }

        Ports.Clear();
        foreach (var p in found.OrderBy(PortOrder).ThenBy(p => p))
            Ports.Add(p);

        // Seed selections if empty
        if (Ports.Count > 0 && string.IsNullOrWhiteSpace(SelectedPortBus0))
            SelectedPortBus0 = Ports[0];

        if (Ports.Count > 1 && string.IsNullOrWhiteSpace(SelectedPortBus1))
            SelectedPortBus1 = Ports.Count > 1 ? Ports[1] : Ports[0];

        StatusText = Ports.Count == 0 ? "No serial ports found." : $"Found {Ports.Count} port(s).";
    }

    private async Task AssignBusesAsync()
    {
        var selected = Flowers.Where(f => f.AssignSelected).ToList();
        if (selected.Count == 0) return;

        // Ask the dialog which bus to use
        var chosenBusId = await AssignBusesInteraction.Handle(selected);
        if (string.IsNullOrWhiteSpace(chosenBusId)) return;

        // Update all selected
        foreach (var f in selected)
        {
            f.BusId = chosenBusId!;
            f.AssignSelected = false; // clear selection after apply
            await _flowerService.UpdateAsync(f);
        }
        await _flowerService.SaveAsync();

        StatusText = $"Assigned {selected.Count} flower(s) to {chosenBusId}.";
    }

    private async Task LoadShowAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "opening-loop.json");
        if (!File.Exists(path))
        {
            await AppendAsync("No show file found (opening-loop.json).\n");
            return;
        }

        _project = await _showStore.LoadAsync(path);
        StatusText = $"Loaded show: {_project.Title}";
        await AppendAsync($"Loaded show '{_project.Title}'\n");
    }

    private async Task PlayAsync()
    {
        if (_project is null) { await AppendAsync("No show loaded.\n"); return; }

        _ = Task.Run(async () =>
        {
            await AppendAsync($"PLAY: {_project!.Title}\n");
            await _player.PlayAsync(_project!);
        });
    }

    private void Stop()
    {
        _player.Stop();
        _ = AppendAsync("STOP\n");
    }

    // ========= Flowers: Load/Save =========
    private async Task LoadFlowersAsync()
    {
        await _flowerService.LoadAsync();
        StatusText = $"Loaded {Flowers.Count} flowers.";
        WireFleetObservers();
    }

    private async Task SaveFlowersAsync()
    {
        await _flowerService.SaveAsync();
        StatusText = $"Saved {Flowers.Count} flowers.";
    }

    // ========= Flowers: Single Actions =========
    private async Task ConnectFlowerAsync()
    {
        var flower = SelectedFlower;
        if (flower is null) return;

        await AppendAsync($"Connecting to flower Id={flower.Id}...\n");
        await Task.Delay(300); // TODO: real connect/ping on flower.BusId
        flower.ConnectionStatus = ConnectionStatus.Connected;
        await _flowerService.UpdateAsync(flower);
        await SaveFlowersAsync();

        RaiseConnectStatesChanged();
        await AppendAsync($"Connected to flower Id={flower.Id}.\n");
    }

    private async Task DisconnectFlowerAsync()
    {
        var flower = SelectedFlower;
        if (flower is null) return;

        await AppendAsync($"Disconnecting flower Id={flower.Id}...\n");
        flower.ConnectionStatus = ConnectionStatus.Disconnected;
        await _flowerService.UpdateAsync(flower);
        await SaveFlowersAsync();

        RaiseConnectStatesChanged();
        await AppendAsync($"Disconnected flower Id={flower.Id}.\n");
    }

    private async Task ReconnectFlowerAsync()
    {
        var flower = SelectedFlower;
        if (flower is null) return;

        await AppendAsync($"Reconnecting flower Id={flower.Id}...\n");
        flower.ConnectionStatus = ConnectionStatus.Disconnected;
        await _flowerService.UpdateAsync(flower);
        await ConnectFlowerAsync();
    }

    // ========= Flowers: Fleet Actions =========
    private async Task ConnectAllFlowersAsync()
    {
        foreach (var flower in Flowers.ToList())
        {
            SelectedFlower = flower;
            await ConnectFlowerAsync();
        }
        SelectedFlower = null;
        RaiseFleetStatesChanged();
    }

    private async Task DisconnectAllFlowersAsync()
    {
        foreach (var flower in Flowers.ToList())
        {
            SelectedFlower = flower;
            await DisconnectFlowerAsync();
        }
        SelectedFlower = null;
        RaiseFleetStatesChanged();
    }

    // ========= Flowers: CRUD via dialogs =========
    private async Task AddFlowerAsync()
    {
        var result = await AddFlowerInteraction.Handle(Unit.Default);
        if (result is null) return;

        if (Flowers.Any(f => f.Id == result.Id))
        {
            await AppendAsync($"Duplicate Id {result.Id}; not added.\n");
            return;
        }

        await _flowerService.AddAsync(result);
        await SaveFlowersAsync();

        StatusText = $"Added flower {result.Id}.";
        await AppendAsync($"Added flower {result.Id}.\n");
    }

    private async Task UpdateFlowerAsync()
    {
        if (SelectedFlower is null) return;

        var updated = await UpdateFlowerInteraction.Handle(SelectedFlower);
        if (updated is null) return;

        var changedId = updated.Id != SelectedFlower.Id;
        if (changedId && Flowers.Any(f => f.Id == updated.Id))
        {
            await AppendAsync($"Duplicate Id {updated.Id}; not updated.\n");
            return;
        }

        var ok = await _flowerService.UpdateAsync(updated);
        if (!ok)
        {
            await AppendAsync($"Failed to update flower {SelectedFlower.Id}.\n");
            return;
        }

        await _flowerService.SaveAsync();
        SelectedFlower = Flowers.FirstOrDefault(f => f.Id == updated.Id);

        StatusText = $"Updated flower {updated.Id}.";
        await AppendAsync($"Updated flower {updated.Id}.\n");
    }

    private async Task DeleteSelectedFlowerAsync()
    {
        if (SelectedFlower is null) return;

        var id = SelectedFlower.Id;
        var ok = await _flowerService.DeleteAsync(id);
        if (!ok)
        {
            await AppendAsync($"Failed to delete flower {id}.\n");
            return;
        }

        SelectedFlower = null;
        await SaveFlowersAsync();

        StatusText = $"Deleted flower {id}.";
    }

    // ========= UI Helpers =========
    private void RaiseConnectStatesChanged()
    {
        this.RaisePropertyChanged(nameof(CanConnect));
        this.RaisePropertyChanged(nameof(CanDisconnect));
        this.RaisePropertyChanged(nameof(CanReconnect));
        this.RaisePropertyChanged(nameof(DisabledConnect));
        this.RaisePropertyChanged(nameof(SelectedFlowerInfo));
    }

    private void RaiseFleetStatesChanged()
    {
        this.RaisePropertyChanged(nameof(CanConnectAll));
        this.RaisePropertyChanged(nameof(CanDisconnectAll));
        this.RaisePropertyChanged(nameof(DisabledConnectAll));
    }

    private Task AppendAsync(string text) =>
        Dispatcher.UIThread.InvokeAsync(() => { MonitorText += text; }).GetTask();

    // ========= Fleet Observer Wiring =========
    private void WireFleetObservers()
    {
        _flowersChangedSub?.Dispose();
        foreach (var d in _itemStatusSubs) d.Dispose();
        _itemStatusSubs.Clear();

        if (Flowers is INotifyCollectionChanged incc)
        {
            _flowersChangedSub =
                Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                        h => incc.CollectionChanged += h,
                        h => incc.CollectionChanged -= h)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ =>
                    {
                        RewireItemSubscriptions();
                        RaiseFleetStatesChanged();
                    });
        }

        RewireItemSubscriptions();
        RaiseFleetStatesChanged();
    }

    private void RewireItemSubscriptions()
    {
        foreach (var d in _itemStatusSubs) d.Dispose();
        _itemStatusSubs.Clear();

        foreach (var f in Flowers)
            SubscribeItemStatus(f);
    }

    private void SubscribeItemStatus(FlowerUnit f)
    {
        if (f is INotifyPropertyChanged inpc)
        {
            var sub =
                Observable.FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                        h => (s, e) => h(e),
                        h => inpc.PropertyChanged += h,
                        h => inpc.PropertyChanged -= h)
                    .Where(e => string.IsNullOrEmpty(e.PropertyName) ||
                                e.PropertyName == nameof(FlowerUnit.ConnectionStatus))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => RaiseFleetStatesChanged());

            _itemStatusSubs.Add(sub);
        }
    }

    // ========= IDisposable =========
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _flowersChangedSub?.Dispose();
        _selectedFlowerSub?.Dispose();
        foreach (var d in _itemStatusSubs) d.Dispose();
        _itemStatusSubs.Clear();
        _hasFlowers?.Dispose();
    }
}
