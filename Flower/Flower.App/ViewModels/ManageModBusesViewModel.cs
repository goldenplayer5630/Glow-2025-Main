using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Enums;
using Flower.Core.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Flower.App.ViewModels
{
    public sealed class ManageModBusesViewModel : ViewModelBase, IManageModBusesViewModel
    {
        private readonly IBusConfigService _busCfg;
        private readonly IBusDirectory _busDir;
        private readonly IUiLogService? _uiLog;
        private readonly IModBusConnectionTester? _tester;

        private System.Collections.Specialized.INotifyCollectionChanged? _busCfgNotifier;
        private bool _disposed;

        private readonly ObservableCollection<BusConfig> _modBuses = new();
        private readonly ReadOnlyObservableCollection<BusConfig> _modBusesRo;
        public ReadOnlyObservableCollection<BusConfig> Buses => _modBusesRo;

        public IReadOnlyList<string> BusIdOptions { get; } =
            Enumerable.Range(0, 8).Select(i => $"bus{i}").ToList();

        private BusConfig? _selected;
        public BusConfig? Selected
        {
            get => _selected;
            set => this.RaiseAndSetIfChanged(ref _selected, value);
        }

        // Editors
        private string _editBusId = "bus0";
        public string EditBusId
        {
            get => _editBusId;
            set => this.RaiseAndSetIfChanged(ref _editBusId, value);
        }

        private string? _editHost = "192.168.1.200";
        public string? EditHost
        {
            get => _editHost;
            set => this.RaiseAndSetIfChanged(ref _editHost, value);
        }

        private int _editPort = 502;
        public int EditPort
        {
            get => _editPort;
            set => this.RaiseAndSetIfChanged(ref _editPort, value);
        }

        private byte _editUnitId = 1;
        public byte EditUnitId
        {
            get => _editUnitId;
            set => this.RaiseAndSetIfChanged(ref _editUnitId, value);
        }

        private int _editTimeoutMs = 2000;
        public int EditTimeoutMs
        {
            get => _editTimeoutMs;
            set => this.RaiseAndSetIfChanged(ref _editTimeoutMs, value);
        }

        private string _status = "Configure Modbus busses, then connect.";
        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        // Commands
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> AddCommand { get; }
        public ReactiveCommand<Unit, Unit> UpdateCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteCommand { get; }
        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        public ReactiveCommand<Unit, Unit> DisconnectCommand { get; }
        public ReactiveCommand<Unit, Unit> TestCommand { get; }
        public ReactiveCommand<Unit, Unit> ConnectAllCommand { get; }
        public ReactiveCommand<Unit, Unit> DisconnectAllCommand { get; }

        public ManageModBusesViewModel(
            IBusConfigService busCfg,
            IBusDirectory busDir,
            IUiLogService? uiLog = null,
            IModBusConnectionTester? tester = null)
        {
            _busCfg = busCfg ?? throw new ArgumentNullException(nameof(busCfg));
            _busDir = busDir ?? throw new ArgumentNullException(nameof(busDir));
            _uiLog = uiLog;
            _tester = tester;

            _modBusesRo = new ReadOnlyObservableCollection<BusConfig>(_modBuses);

            // Initial fill + keep in sync (ReadOnlyObservableCollection hides CollectionChanged; use interface)
            RefreshModbusList();
            if (_busCfg.Buses is System.Collections.Specialized.INotifyCollectionChanged incc)
            {
                _busCfgNotifier = incc;
                _busCfgNotifier.CollectionChanged += OnBusCfgCollectionChanged;
            }

            SaveCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _busCfg.SaveAsync();
                Status = $"Saved {Buses.Count} Modbus bus config(s).";
                _uiLog?.Info(Status);
            });

            AddCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var err = Validate(EditBusId, EditHost, EditPort, EditUnitId, EditTimeoutMs);
                if (err != null) { Status = err; _uiLog?.Error(err, new InvalidOperationException(err)); return; }

                if (_busCfg.Buses.Any(b => string.Equals(b.BusId, EditBusId, StringComparison.OrdinalIgnoreCase)))
                {
                    Status = $"BusId '{EditBusId}' already exists.";
                    _uiLog?.Error(Status, new InvalidOperationException(Status));
                    return;
                }

                var bus = new BusConfig(EditBusId.Trim());
                bus.CreateModbusTcp(EditHost!.Trim(), EditPort, EditUnitId);
                bus.ModbusConnectTimeoutMs = EditTimeoutMs;

                await _busCfg.AddAsync(bus);
                await _busCfg.SaveAsync();

                Status = $"Added {bus.BusId} (Modbus TCP {bus.ModbusHost}:{bus.ModbusPort}, Unit {bus.ModbusUnitId}).";
                _uiLog?.Info(Status);

                SuggestNextFreeBusId();
            });

            UpdateCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var selected = Selected;
                if (selected is null) { Status = "No bus selected."; _uiLog?.Error(Status, new InvalidOperationException(Status)); return; }

                var host = EditHost ?? selected.ModbusHost;
                var err = Validate(selected.BusId, host, EditPort, EditUnitId, EditTimeoutMs);
                if (err != null) { Status = err; _uiLog?.Error(err, new InvalidOperationException(err)); return; }

                selected.BusType = BusType.ModbusTcp;
                selected.ModbusHost = host!.Trim();
                selected.ModbusPort = EditPort;
                selected.ModbusUnitId = EditUnitId;
                selected.ModbusConnectTimeoutMs = EditTimeoutMs;

                selected.ConnectionStatus = ConnectionStatus.Disconnected;
                await _busDir.DisconnectAsync(selected.BusId).ConfigureAwait(false);

                await _busCfg.UpdateAsync(selected);
                await _busCfg.SaveAsync();

                Status = $"Updated {selected.BusId}.";
                _uiLog?.Info(Status);
            });

            DeleteCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var selected = Selected;
                if (selected is null) { Status = "No bus selected."; _uiLog?.Error(Status, new InvalidOperationException(Status)); return; }

                await _busDir.DisconnectAsync(selected.BusId).ConfigureAwait(false);
                await _busCfg.DeleteAsync(selected.BusId);
                await _busCfg.SaveAsync();

                Status = $"Deleted {selected.BusId}.";
                _uiLog?.Info(Status);

                SuggestNextFreeBusId();
            });

            ConnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var selected = Selected;
                if (selected is null) { Status = "No bus selected."; _uiLog?.Error(Status, new InvalidOperationException(Status)); return; }

                try
                {
                    await _busDir.ConnectAsync(selected);
                    selected.ConnectionStatus = ConnectionStatus.Connected;

                    await _busCfg.UpdateAsync(selected);
                    await _busCfg.SaveAsync();

                    Status = $"Connected {selected.BusId} → {selected.ModbusHost}:{selected.ModbusPort} (Unit {selected.ModbusUnitId}).";
                    _uiLog?.Info(Status);
                }
                catch (Exception ex)
                {
                    selected.ConnectionStatus = ConnectionStatus.Degraded;
                    Status = $"Connect failed: {ex.Message}";
                    _uiLog?.Error(Status, ex);
                }
            });

            DisconnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var selected = Selected;
                if (selected is null) { Status = "No bus selected."; _uiLog?.Error(Status, new InvalidOperationException(Status)); return; }

                try
                {
                    await _busDir.DisconnectAsync(selected.BusId);
                    selected.ConnectionStatus = ConnectionStatus.Disconnected;

                    await _busCfg.UpdateAsync(selected);
                    await _busCfg.SaveAsync();

                    Status = $"Disconnected {selected.BusId}.";
                    _uiLog?.Info(Status);
                }
                catch (Exception ex)
                {
                    selected.ConnectionStatus = ConnectionStatus.Degraded;
                    Status = $"Disconnect failed: {ex.Message}";
                    _uiLog?.Error(Status, ex);
                }
            });

            TestCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var bus = Selected ?? new BusConfig(EditBusId.Trim());
                bus.CreateModbusTcp((EditHost ?? bus.ModbusHost ?? "").Trim(), EditPort, EditUnitId);
                bus.ModbusConnectTimeoutMs = EditTimeoutMs;

                if (_tester is null)
                {
                    Status = "No Modbus tester service registered (IModBusConnectionTester).";
                    _uiLog?.Error(Status, new InvalidOperationException(Status));
                    return;
                }

                try
                {
                    var (ok, msg) = await _tester.TestAsync(bus);
                    Status = msg;
                    if (ok) _uiLog?.Info(msg);
                    else _uiLog?.Error(msg, new InvalidOperationException(msg));
                }
                catch (Exception ex)
                {
                    Status = $"Test failed: {ex.Message}";
                    _uiLog?.Error(Status, ex);
                }
            });

            ConnectAllCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                int ok = 0, fail = 0;
                foreach (var b in Buses)
                {
                    try
                    {
                        await _busDir.ConnectAsync(b);
                        b.ConnectionStatus = ConnectionStatus.Connected;
                        ok++;
                    }
                    catch
                    {
                        b.ConnectionStatus = ConnectionStatus.Degraded;
                        fail++;
                    }
                }

                await _busCfg.SaveAsync();
                Status = $"Connect all Modbus: {ok} ok, {fail} failed.";
                _uiLog?.Info(Status);
            });

            DisconnectAllCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                foreach (var b in Buses)
                {
                    await _busDir.DisconnectAsync(b.BusId);
                    b.ConnectionStatus = ConnectionStatus.Disconnected;
                }

                await _busCfg.SaveAsync();
                Status = "Disconnected all Modbus busses.";
                _uiLog?.Info(Status);
            });

            this.WhenAnyValue(vm => vm.Selected)
                .Where(x => x != null)
                .Subscribe(x =>
                {
                    EditBusId = x!.BusId;
                    EditHost = x.ModbusHost;
                    EditPort = x.ModbusPort;
                    EditUnitId = x.ModbusUnitId;
                    EditTimeoutMs = x.ModbusConnectTimeoutMs;
                });

            SuggestNextFreeBusId();
        }

        private void OnBusCfgCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            => RefreshModbusList();

        private void RefreshModbusList()
        {
            _modBuses.Clear();
            foreach (var b in _busCfg.Buses.Where(x => x.BusType == BusType.ModbusTcp))
                _modBuses.Add(b);

            if (Selected is not null && !_modBuses.Contains(Selected))
                Selected = _modBuses.FirstOrDefault();
        }

        private void SuggestNextFreeBusId()
        {
            var used = _busCfg.Buses.Select(b => b.BusId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var suggestion = BusIdOptions.FirstOrDefault(opt => !used.Contains(opt)) ?? $"bus{used.Count}";
            EditBusId = suggestion;
        }

        private static string? Validate(string? busId, string? host, int port, byte unitId, int timeoutMs)
        {
            if (string.IsNullOrWhiteSpace(busId)) return "BusId is required.";
            if (!busId.StartsWith("bus", StringComparison.OrdinalIgnoreCase)) return "BusId must start with 'bus'.";
            if (busId.Length < 4 || !int.TryParse(busId.AsSpan(3), out _)) return "BusId must look like 'bus0', 'bus1', …";

            if (string.IsNullOrWhiteSpace(host)) return "Modbus Host (IP/DNS) is required.";
            if (port <= 0 || port > 65535) return "Modbus port must be 1..65535.";
            if (unitId < 1) return "UnitId must be >= 1.";
            if (timeoutMs < 100) return "Timeout must be >= 100ms.";
            return null;
        }

        public void Close()
        {
            if (Selected is not null)
            {
                EditBusId = Selected.BusId;
                EditHost = Selected.ModbusHost;
                EditPort = Selected.ModbusPort;
                EditUnitId = Selected.ModbusUnitId;
                EditTimeoutMs = Selected.ModbusConnectTimeoutMs;
                Status = $"Reverted changes for {Selected.BusId}.";
            }
            else
            {
                SuggestNextFreeBusId();
                EditHost = "192.168.1.200";
                EditPort = 502;
                EditUnitId = 1;
                EditTimeoutMs = 2000;
                Status = "Reverted changes.";
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_busCfgNotifier is not null)
                _busCfgNotifier.CollectionChanged -= OnBusCfgCollectionChanged;
        }
    }
}
