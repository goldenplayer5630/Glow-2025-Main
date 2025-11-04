using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Enums;
using Flower.Core.Models;
using ReactiveUI;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public sealed class ManageBusesViewModel : ViewModelBase, IManageBusesViewModel, IDisposable
    {
        private readonly IBusConfigService _busCfg;
        private readonly IBusDirectory _busDir;
        private bool _disposed;

        public ReadOnlyObservableCollection<BusConfig> Buses => _busCfg.Buses;
        public ObservableCollection<string> Ports { get; } = new();

        // Pre-filled BusId suggestions for touch UI
        public IReadOnlyList<string> BusIdOptions { get; } =
            Enumerable.Range(0, 8).Select(i => $"bus{i}").ToList();

        private BusConfig? _selected;
        public BusConfig? Selected
        {
            get => _selected;
            set => this.RaiseAndSetIfChanged(ref _selected, value);
        }

        // Editors for Add / Update
        private string _editBusId = "bus0";
        public string EditBusId
        {
            get => _editBusId;
            set => this.RaiseAndSetIfChanged(ref _editBusId, value);
        }

        private string? _editPort;
        public string? EditPort
        {
            get => _editPort;
            set => this.RaiseAndSetIfChanged(ref _editPort, value);
        }

        private int _editBaud = 9600;
        public int EditBaud
        {
            get => _editBaud;
            set => this.RaiseAndSetIfChanged(ref _editBaud, value);
        } 

        private string _status = "Configure busses, then connect.";
        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        // Commands
        public ReactiveCommand<Unit, Unit> RefreshPortsCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> AddCommand { get; }
        public ReactiveCommand<Unit, Unit> UpdateCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteCommand { get; }
        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        public ReactiveCommand<Unit, Unit> DisconnectCommand { get; }

        // Optional bulk helpers for convenience
        public ReactiveCommand<Unit, Unit> ConnectAllCommand { get; }
        public ReactiveCommand<Unit, Unit> DisconnectAllCommand { get; }

        public ManageBusesViewModel(IBusConfigService busCfg, IBusDirectory busDir)
        {
            _busCfg = busCfg;
            _busDir = busDir;
            _editBaud = 9600;

            RefreshPortsCommand = ReactiveCommand.Create(RefreshPorts);

            LoadCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _busCfg.LoadAsync();
                Status = $"Loaded {Buses.Count} bus config(s).";
                SuggestNextFreeBusId();
            });

            SaveCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _busCfg.SaveAsync();
                Status = $"Saved {Buses.Count} bus config(s).";
            });

            AddCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var err = Validate(EditBusId, EditPort, EditBaud, adding: true);
                if (err != null) { Status = err; return; }

                if (Buses.Any(b => string.Equals(b.BusId, EditBusId, StringComparison.OrdinalIgnoreCase)))
                {
                    Status = $"BusId '{EditBusId}' already exists.";
                    return;
                }

                var cfg = new BusConfig(EditBusId.Trim(), EditPort!.Trim(), EditBaud)
                { ConnectionStatus = ConnectionStatus.Disconnected };

                await _busCfg.AddAsync(cfg);
                await _busCfg.SaveAsync();
                Status = $"Added {cfg.BusId}.";
                SuggestNextFreeBusId();
            });

            UpdateCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (Selected is null) { Status = "No bus selected."; return; }
                var err = Validate(Selected.BusId, EditPort ?? Selected.Port, EditBaud, adding: false);
                if (err != null) { Status = err; return; }

                Selected.Port = (EditPort ?? Selected.Port)!.Trim();
                Selected.Baud = EditBaud;

                await _busCfg.UpdateAsync(Selected);
                await _busCfg.SaveAsync();
                Status = $"Updated {Selected.BusId}.";
            });

            DeleteCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (Selected is null) { Status = "No bus selected."; return; }
                await _busDir.DisconnectAsync(Selected.BusId).ConfigureAwait(false); // best effort
                await _busCfg.DeleteAsync(Selected.BusId);
                await _busCfg.SaveAsync();
                Status = $"Deleted {Selected.BusId}.";
                SuggestNextFreeBusId();
            });

            ConnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (Selected is null) { Status = "No bus selected."; return; }
                try
                {
                    await _busDir.ConnectAsync(Selected);
                    Selected.ConnectionStatus = ConnectionStatus.Connected;
                    await _busCfg.UpdateAsync(Selected);
                    await _busCfg.SaveAsync();
                    Status = $"Connected {Selected.BusId} → {Selected.Port} @ {Selected.Baud}.";
                }
                catch (Exception ex)
                {
                    Selected.ConnectionStatus = ConnectionStatus.Degraded;
                    Status = $"Connect failed: {ex.Message}";
                }
            });

            DisconnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (Selected is null) { Status = "No bus selected."; return; }
                await _busDir.DisconnectAsync(Selected.BusId);
                Selected.ConnectionStatus = ConnectionStatus.Disconnected;
                await _busCfg.UpdateAsync(Selected);
                await _busCfg.SaveAsync();
                Status = $"Disconnected {Selected.BusId}.";
            });

            ConnectAllCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                int ok = 0, fail = 0;
                foreach (var b in Buses)
                {
                    try { await _busDir.ConnectAsync(b); b.ConnectionStatus = ConnectionStatus.Connected; ok++; }
                    catch { b.ConnectionStatus = ConnectionStatus.Degraded; fail++; }
                }
                await _busCfg.SaveAsync();
                Status = $"Connect all: {ok} ok, {fail} failed.";
            });

            DisconnectAllCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                foreach (var b in Buses)
                {
                    await _busDir.DisconnectAsync(b.BusId);
                    b.ConnectionStatus = ConnectionStatus.Disconnected;
                }
                await _busCfg.SaveAsync();
                Status = "Disconnected all busses.";
            });

            // Update editors when selecting a row
            this.WhenAnyValue(vm => vm.Selected)
                .Where(x => x != null)
                .Subscribe(x =>
                {
                    // BusId is immutable once created (prevents accidental re-keying)
                    EditBusId = x!.BusId;
                    EditPort = x.Port;
                    EditBaud = x.Baud;
                });

            RefreshPorts();
            _ = LoadCommand.Execute();
        }

        private void SuggestNextFreeBusId()
        {
            var used = Buses.Select(b => b.BusId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var suggestion = BusIdOptions.FirstOrDefault(opt => !used.Contains(opt)) ?? $"bus{used.Count}";
            EditBusId = suggestion;
        }

        private static string? Validate(string? busId, string? port, int baud, bool adding)
        {
            if (string.IsNullOrWhiteSpace(busId)) return "BusId is required.";
            if (!busId.StartsWith("bus", StringComparison.OrdinalIgnoreCase)) return "BusId must start with 'bus'.";
            if (busId.Length < 4 || !int.TryParse(busId.AsSpan(3), out _)) return "BusId must look like 'bus0', 'bus1', …";
            if (string.IsNullOrWhiteSpace(port)) return "Select a serial port.";
            if (baud <= 0) return "Baud must be > 0.";
            return null;
        }

        private void RefreshPorts()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Create a temporary instance to call the instance method GetPortNames()
                using (var sps = new SerialPortStream())
                {
                    foreach (var p in sps.GetPortNames())
                        if (!string.IsNullOrWhiteSpace(p)) set.Add(p.Trim());
                }
            }
            catch { }
            try { foreach (var p in SerialPort.GetPortNames()) if (!string.IsNullOrWhiteSpace(p)) set.Add(p.Trim()); } catch { }

            Ports.Clear();
            foreach (var p in set.OrderBy(PortOrder).ThenBy(p => p)) Ports.Add(p);

            if (Selected is not null && !string.IsNullOrWhiteSpace(Selected.Port) && !Ports.Contains(Selected.Port))
                Ports.Add(Selected.Port); // show stale port if present in config

            Status = Ports.Count == 0 ? "No serial ports found." : $"Found {Ports.Count} port(s).";
        }

        private static int PortOrder(string name)
            => name.StartsWith("COM", StringComparison.OrdinalIgnoreCase) && int.TryParse(name[3..], out var n) ? n : int.MaxValue;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        public async Task ConfirmAsync()
        {
            // If no selection -> behave like Add
            if (Selected is null)
            {
                var err = Validate(EditBusId, EditPort, EditBaud, adding: true);
                if (err != null) { Status = err; return; }

                if (Buses.Any(b => string.Equals(b.BusId, EditBusId, StringComparison.OrdinalIgnoreCase)))
                {
                    Status = $"BusId '{EditBusId}' already exists.";
                    return;
                }

                var cfg = new BusConfig(EditBusId.Trim(), (EditPort ?? string.Empty).Trim(), EditBaud)
                {
                    ConnectionStatus = ConnectionStatus.Disconnected
                };

                await _busCfg.AddAsync(cfg);
                await _busCfg.SaveAsync();

                Status = $"Added {cfg.BusId}.";
                SuggestNextFreeBusId();
                return;
            }

            // Selection exists -> behave like Update
            var updatePort = (EditPort ?? Selected.Port);
            var err2 = Validate(Selected.BusId, updatePort, EditBaud, adding: false);
            if (err2 != null) { Status = err2; return; }

            Selected.Port = updatePort!.Trim();
            Selected.Baud = EditBaud;

            await _busCfg.UpdateAsync(Selected);
            await _busCfg.SaveAsync();

            Status = $"Updated {Selected.BusId}.";
        }

        public void Cancel()
        {
            if (Selected is not null)
            {
                // Revert editor to selected item
                EditBusId = Selected.BusId;   // immutable anyway in UI
                EditPort = Selected.Port;
                EditBaud = Selected.Baud;
                Status = $"Reverted changes for {Selected.BusId}.";
            }
            else
            {
                // Revert to defaults when nothing is selected
                SuggestNextFreeBusId();
                EditPort = Ports.FirstOrDefault();
                EditBaud = 9600;
                Status = "Reverted changes.";
            }
        }
    }
}
