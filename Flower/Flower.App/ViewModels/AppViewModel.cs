// Flower.App/ViewModels/AppViewModel.cs
using Avalonia.Threading;
using Flower.App.Views;
using Flower.Core.Abstractions;
using Flower.Core.Models;
using Flower.Infrastructure.Persistence;
using ReactiveUI;
using RJCP.IO.Ports;                    // for SerialPortStream.GetPortNames()
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Flower.App.ViewModels;

public sealed class AppViewModel : ViewModelBase, IAppViewModel
{
    private readonly ISerialPort _serial;
    private readonly ShowPlayer _player;
    private ShowProject? _project;

    // ---- Bindable state
    public ObservableCollection<string> Ports { get; } = new();

    private object? _currentPage;
    public object? CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    private string? _selectedPort;
    public string? SelectedPort
    {
        get => _selectedPort;
        set => this.RaiseAndSetIfChanged(ref _selectedPort, value);
    }

    private string _statusText = "Select a port and click Connect.";
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

    // ---- Commands
    public ReactiveCommand<Unit, Unit> RefreshPortsCommand { get; }
    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadShowCommand { get; }
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    public ReactiveCommand<Unit, Unit> OpenShowCreatorCommand { get; }
    public Interaction<Unit, Unit> OpenShowCreatorInteraction { get; } = new();

    public AppViewModel(
        ISerialPort serial,
        ShowPlayer player) 
    {
        _serial = serial;
        _player = player;

        RefreshPortsCommand = ReactiveCommand.Create(RefreshPorts);
        ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync);
        LoadShowCommand = ReactiveCommand.CreateFromTask(LoadShowAsync);
        PlayCommand = ReactiveCommand.CreateFromTask(PlayAsync);
        StopCommand = ReactiveCommand.Create(Stop);
        OpenShowCreatorCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await OpenShowCreatorInteraction.Handle(Unit.Default);
        });

        RefreshPorts(); // initial scan
    }

    // ---- Behaviors
    private void RefreshPorts()
    {
        Ports.Clear();
        // Create an instance of SerialPortStream to call GetPortNames()
        using (var portStream = new SerialPortStream())
        {
            string[] availablePorts = portStream.GetPortNames();
            for (int i = 0; i < availablePorts.Length; i++)
            {
                string? p = availablePorts[i];
                Ports.Add(p);
            }
        }

        if (Ports.Count > 0) SelectedPort = Ports[0];
        StatusText = "Ports refreshed.";
    }

    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedPort))
        {
            await AppendAsync("No port selected.\n");
            return;
        }

        await _serial.OpenAsync(SelectedPort, 115200);
        StatusText = $"Connected: {SelectedPort}";
        await AppendAsync($"Connected to {SelectedPort}\n");
    }

    private async Task LoadShowAsync()
    {
        // MVP: load a default show file from beside the executable
        var path = Path.Combine(AppContext.BaseDirectory, "opening-loop.json");
        if (!File.Exists(path))
        {
            await AppendAsync("No show file found (opening-loop.json).\n");
            return;
        }

        _project = await ShowStore.LoadAsync(path);
        StatusText = $"Loaded show: {_project.Title}";
        await AppendAsync($"Loaded show '{_project.Title}'\n");
    }

    private async Task PlayAsync()
    {
        if (_project is null) { await AppendAsync("No show loaded.\n"); return; }
        if (!_serial.IsOpen) { await AppendAsync("Not connected.\n"); return; }

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

    private async Task AppendAsync(string text) =>
       await Dispatcher.UIThread.InvokeAsync(() => { MonitorText += text; });
}
