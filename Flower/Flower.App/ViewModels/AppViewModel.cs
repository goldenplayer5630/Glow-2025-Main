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
    private readonly ICommandService _commandService;
    private readonly IShowPlayerService _player;
    private readonly IShowProjectStore _showStore;
    private readonly IFlowerService _flowerService;
    private readonly IUiLogService _uiLog;

    private ShowProject? _project;

    // Subscriptions (fleet + selection)
    private IDisposable? _flowersChangedSub;
    private readonly List<IDisposable> _itemStatusSubs = new();   // for connection-status watchers (fleet states)
    private IDisposable? _selectedFlowerSub;
    private readonly ObservableAsPropertyHelper<bool> _hasFlowers;

    // NEW: OAPH for CanAssignBuses (replaces manual backing field)
    private readonly ObservableAsPropertyHelper<bool> _canAssignBusesOaph;

    // For internal per-item AssignSelected watchers built by anySelected pipeline
    private readonly List<IDisposable> _assignSubs = new();

    private bool _disposed;

    // ========= Bindable State =========
    public ObservableCollection<string> Ports { get; } = new();

    private object? _currentPage;
    public object? CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    private string _statusText;
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
                                   e.PropertyName == nameof(FlowerUnit.ConnectionStatus) ||
                                   e.PropertyName == nameof(FlowerUnit.BusId) ||
                                   e.PropertyName == nameof(FlowerUnit.CurrentBrightness) ||
                                   e.PropertyName == nameof(FlowerUnit.FlowerStatus))
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
    public bool CanConnect => SelectedFlower?.ConnectionStatus == ConnectionStatus.Disconnected && !string.IsNullOrEmpty(SelectedFlower?.BusId);
    public bool CanDisconnect => SelectedFlower?.ConnectionStatus == ConnectionStatus.Connected;
    public bool CanReconnect => SelectedFlower?.ConnectionStatus == ConnectionStatus.Degraded && !string.IsNullOrEmpty(SelectedFlower?.BusId);
    public bool DisabledConnect => !CanConnect && !CanDisconnect && !CanReconnect;
    public bool CanSendCommand => SelectedFlower?.ConnectionStatus == ConnectionStatus.Connected && !string.IsNullOrEmpty(SelectedFlower?.BusId);

    // ========= Visibility Helpers: Fleet =========
    public bool HasFlowers => _hasFlowers.Value;

    public bool CanConnectAll => HasFlowers && Flowers.Any(f => f.ConnectionStatus != ConnectionStatus.Connected);
    public bool CanDisconnectAll => HasFlowers && Flowers.All(f => f.ConnectionStatus == ConnectionStatus.Connected);
    public bool DisabledConnectAll => !HasFlowers || (!CanConnectAll && !CanDisconnectAll);

    // NEW: read-only property backed by OAPH
    public bool CanAssignBuses => _canAssignBusesOaph.Value;

    // ========= Commands =========
    public ReactiveCommand<Unit, Unit> LoadShowCommand { get; }
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }
    public ReactiveCommand<Unit, Unit> EmergencyStopCommand { get; }


    public ReactiveCommand<Unit, Unit> ConnectFlowerCommand { get; }
    public ReactiveCommand<Unit, Unit> DisconnectFlowerCommand { get; }
    public ReactiveCommand<Unit, Unit> ReconnectFlowerCommand { get; }

    public ReactiveCommand<Unit, Unit> ConnectAllFlowersCommand { get; }
    public ReactiveCommand<Unit, Unit> DisconnectAllFlowersCommand { get; }
    public ReactiveCommand<Unit, Unit> AssignBusesCommand { get; }

    public ReactiveCommand<Unit, Unit> SendCommandToFlowerCommand { get; }
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
    public Interaction<FlowerUnit, string?> SendCommandToFlowerInteraction { get; } = new();
    public Interaction<Unit, ShowProject?> LoadShowInteraction { get; } = new();


    // ========= Ctor =========
    public AppViewModel(
        ICommandService commandService,
        IShowPlayerService player,
        IShowProjectStore showStore,
        IFlowerService flowerService,
        IUiLogService uiLog)
    {
        _statusText = "Ready.";
        _commandService = commandService;
        _player = player;
        _showStore = showStore;
        _flowerService = flowerService;
        _uiLog = uiLog;

        // Subscribe log events
        uiLog.InfoPublished += OnUiLogInfo;
        uiLog.ErrorPublished += OnUiLogError;

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

        _hasFlowers = hasFlowersObs.ToProperty(this, vm => vm.HasFlowers, initialValue: Flowers.Count > 0, scheduler: RxApp.MainThreadScheduler);

        // === AnySelected (reacts to collection and per-item AssignSelected changes) ===
        var collectionChanges =
            Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => ((INotifyCollectionChanged)Flowers).CollectionChanged += h,
                h => ((INotifyCollectionChanged)Flowers).CollectionChanged -= h)
            .StartWith(default(EventPattern<NotifyCollectionChangedEventArgs>)); // seed once

        var anySelectedObs =
            collectionChanges
                .Select(_ =>
                {
                    // tear down & rebuild per-item subscriptions
                    foreach (var d in _assignSubs) d.Dispose();
                    _assignSubs.Clear();

                    var itemChangedStreams = Flowers
                        .OfType<INotifyPropertyChanged>()
                        .Select(inpc =>
                            Observable.FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                                    h => (s, e) => h(e),
                                    h => inpc.PropertyChanged += h,
                                    h => inpc.PropertyChanged -= h)
                                .Where(e => e.PropertyName == nameof(FlowerUnit.AssignSelected))
                                .Select(__ => Unit.Default)
                        )
                        .ToList();

                    // keep strong refs so we can dispose later
                    foreach (var s in itemChangedStreams)
                        _assignSubs.Add(s.Subscribe(_2 => { /* merged below */ }));

                    // emit current state once, then on any row change
                    return Observable.Merge(itemChangedStreams)
                                     .StartWith(Unit.Default)
                                     .Select(_2 => Flowers.Any(f => f.AssignSelected));
                })
                .Switch()
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler);

        // Bind to read-only property (no manual setter anywhere)
        _canAssignBusesOaph = anySelectedObs
            .ToProperty(this, vm => vm.CanAssignBuses, initialValue: Flowers.Any(f => f.AssignSelected), scheduler: RxApp.MainThreadScheduler);

        AssignBusesCommand = ReactiveCommand.CreateFromTask(AssignBusesAsync, anySelectedObs);
        EmergencyStopCommand = ReactiveCommand.CreateFromTask(EmergencyStopAsync);

        // === Other Commands ===
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

        SendCommandToFlowerCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var flower = SelectedFlower;
            if (flower is null)
            {
                await AppendAsync("No flower selected.\n");
                return;
            }

            await AppendAsync($"Opening send-command dialog for flower #{flower.Id}...\n");

            var commandResult = await SendCommandToFlowerInteraction.Handle(flower);

            if (string.IsNullOrWhiteSpace(commandResult))
            {
                await AppendAsync("Send canceled.\n");
                return;
            }

            await AppendAsync($"Dialog closed after sending '{commandResult}'.\n");
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

        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
        });

        WireFleetObservers();          // initial wiring to current Flowers collection
        RaiseFleetStatesChanged();
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

        await AppendAsync($"Assigned {selected.Count} flower(s) to {chosenBusId}.\n");
        StatusText = $"Assigned {selected.Count} flower(s) to {chosenBusId}.";
    }

    private async Task LoadShowAsync()
    {
        var result = await LoadShowInteraction.Handle(Unit.Default);
        if (result is null)
        {
            await AppendAsync("Load show cancelled.\n");
            return;
        }

        _project = result;
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

    private async Task EmergencyStopAsync()
    {
        // 1) Stop the running show immediately
        _player.Stop();
        await AppendAsync("EMERGENCY STOP: stopping show and turning off ALL LEDs...\n");

        // 2) Snapshot buses (do NOT enumerate the live collection directly later)
        var buses = Flowers
            .Select(f => f.BusId)
            .Where(b => !string.IsNullOrWhiteSpace(b) && !string.Equals(b, "None", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        if (buses.Count == 0)
        {
            await AppendAsync("No buses assigned; nothing to broadcast to.\n");
            StatusText = "Emergency stop: no buses.";
            return;
        }

        // 3) Broadcast LED:0 per bus (id 0 = broadcast)
        foreach (var bus in buses)
        {
            try
            {
                var broadcast = new FlowerUnit
                {
                    Id = 0, // broadcast
                    BusId = bus,
                    Category = FlowerCategory.SmallTulip,
                    ConnectionStatus = ConnectionStatus.Connected,
                };

                var args = new Dictionary<string, object>
                {
                    // adapt to your LED command’s parameter name(s)
                    ["intensity"] = 0,
                    ["level"] = 0,
                    ["brightness"] = 0,
                };

                var outcome = await _commandService.SendCommandAsync("led.set", broadcast, args);
                await AppendAsync($"Bus {bus}: LED 0 broadcast → {outcome}\n");
            }
            catch (Exception ex)
            {
                await AppendAsync($"Bus {bus}: LED 0 broadcast FAILED: {ex.Message}\n");
            }
        }

        // 4) Snapshot flowers BEFORE updating them, then update safely
        var flowerSnapshot = _flowerService.Flowers.ToList();
        foreach (var flower in flowerSnapshot)
        {
            try
            {
                flower.CurrentBrightness = 0;
                await _flowerService.UpdateAsync(flower);
            }
            catch (Exception ex)
            {
                await AppendAsync($"Flower {flower.Id}: update FAILED: {ex.Message}\n");
            }
        }

        try
        {
            await _flowerService.SaveAsync();
        }
        catch (Exception ex)
        {
            await AppendAsync($"Save FAILED: {ex.Message}\n");
        }

        StatusText = "EMERGENCY STOP sent.";
    }



    // ========= Flowers: Load/Save =========

    private async Task SaveFlowersAsync()
    {
        await _flowerService.SaveAsync();
        StatusText = $"Saved {Flowers.Count} flowers.";
    }

    // ========= Flowers: Single Actions =========

    private async Task ConnectFlowerAsync() => await ConnectFlowerAsync(isReconnect: false);
    private async Task ConnectFlowerAsync(bool isReconnect = false)
    {
        var flower = SelectedFlower;
        if (flower is null) return;

        // guard: a bus must be assigned
        if (string.IsNullOrWhiteSpace(flower.BusId) || string.Equals(flower.BusId, "None", StringComparison.OrdinalIgnoreCase))
        {
            if (isReconnect)
                await AppendAsync($"Cannot reconnect flower Id={flower?.Id}: no BusId assigned.\n");
            else
                await AppendAsync($"Cannot connect flower Id={flower?.Id}: no BusId assigned.\n");
            return;
        }

        if (isReconnect)
            await AppendAsync($"Reconnecting flower Id={flower.Id} on {flower.BusId}...\n");
        else
            await AppendAsync($"Connecting flower Id={flower.Id} on {flower.BusId}...\n");
        try
        {
            // ping has no args
            var args = new Dictionary<string, object>();

            // This enqueues the command; the dispatcher will set state via PingCmd.StateOnAck()
            var outcome = await _commandService.SendCommandAsync("ping", flower, args);

            if (outcome != CommandOutcome.Acked)
                throw new InvalidOperationException($"Ping command outcome: {outcome}");

            RaiseConnectStatesChanged();
            await AppendAsync($"Ping OK → flower Id={flower.Id} is {flower.ConnectionStatus}.\n");
        }
        catch (Exception ex)
        {
            // Mark degraded on failure (mirrors CommandService on-timeout behavior)
            flower.ConnectionStatus = ConnectionStatus.Degraded;
            await _flowerService.UpdateAsync(flower);
            await SaveFlowersAsync();

            RaiseConnectStatesChanged();
            await AppendAsync($"Ping FAILED for flower Id={flower.Id}\n");
            await AppendAsync($"Exception: {ex.Message}\n");
        }
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
        await ConnectFlowerAsync(true);
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
        this.RaisePropertyChanged(nameof(CanSendCommand));
        this.RaisePropertyChanged(nameof(SelectedFlowerInfo));
    }

    private void RaiseFleetStatesChanged()
    {
        this.RaisePropertyChanged(nameof(CanConnectAll));
        this.RaisePropertyChanged(nameof(CanDisconnectAll));
        this.RaisePropertyChanged(nameof(DisabledConnectAll));
    }

    public Task AppendAsync(string text) =>
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
                        // no manual recompute here; OAPH handles CanAssignBuses
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

    private async void OnUiLogInfo(string msg)
    {
        await AppendAsync(msg.EndsWith("\n") ? msg : msg + "\n");
    }

    private async void OnUiLogError(string msg, Exception ex)
    {
        var line = $"{msg} :: {ex.GetType().Name}: {ex.Message}";
        await AppendAsync(line.EndsWith("\n") ? line : line + "\n");
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

        foreach (var d in _assignSubs) d.Dispose();
        _assignSubs.Clear();

        _hasFlowers?.Dispose();
        _canAssignBusesOaph?.Dispose();
    }
}
