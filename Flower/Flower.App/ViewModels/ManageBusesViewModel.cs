using Avalonia;
using Avalonia.Styling;
using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Enums;
using Flower.Core.Models;
using ReactiveUI;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
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

                var cfg = new BusConfig(EditBusId.Trim(), EditPort!.Trim(), EditBaud, ConnectionStatus.Disconnected)
                { ConnectionStatus = ConnectionStatus.Disconnected };

                await _busCfg.AddAsync(cfg);
                await _busCfg.SaveAsync();
                Status = $"Added {cfg.BusId}.";
                SuggestNextFreeBusId();
            });

            UpdateCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var selected = Selected;
                if (selected is null) { Status = "No bus selected."; return; }
                var err = Validate(selected.BusId, EditPort ?? selected.Port, EditBaud, adding: false);
                if (err != null) { Status = err; return; }

                selected.Port = (EditPort ?? selected.Port)!.Trim();
                selected.Baud = EditBaud;

                await _busCfg.UpdateAsync(selected);
                await _busCfg.SaveAsync();
                Status = $"Updated {selected.BusId}.";
            });

            DeleteCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var selected = Selected;
                if (selected is null) { Status = "No bus selected."; return; }
                await _busDir.DisconnectAsync(selected.BusId).ConfigureAwait(false); // best effort
                await _busCfg.DeleteAsync(selected.BusId);
                await _busCfg.SaveAsync();
                Status = $"Deleted {selected.BusId}.";
                SuggestNextFreeBusId();
            });

            ConnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var selected = Selected;
                if (selected is null) { Status = "No bus selected."; return; }
                try
                {
                    await _busDir.ConnectAsync(selected);
                    selected.ConnectionStatus = ConnectionStatus.Connected;
                    await _busCfg.UpdateAsync(selected);
                    await _busCfg.SaveAsync();
                    Status = $"Connected {selected.BusId} → {selected.Port} @ {selected.Baud}.";
                }
                catch (Exception ex)
                {
                    selected.ConnectionStatus = ConnectionStatus.Degraded;
                    Status = $"Connect failed: {ex.Message}";
                }
            });

            DisconnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var selected = Selected;
                if (selected is null) { Status = "No bus selected."; return; }
                try
                {
                    await _busDir.DisconnectAsync(selected.BusId);
                    selected.ConnectionStatus = ConnectionStatus.Disconnected;
                    await _busCfg.UpdateAsync(selected);
                    await _busCfg.SaveAsync();
                    Status = $"Disconnected {selected.BusId}.";
                }
                catch (Exception ex)
                {
                    selected.ConnectionStatus = ConnectionStatus.Degraded;
                    Status = $"Disconnect failed: {ex.Message}";
                }
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

            // 1) RJCP (your version has instance GetPortNames())
            try
            {
                using var sps = new RJCP.IO.Ports.SerialPortStream();
                foreach (var p in sps.GetPortNames())
                    if (!string.IsNullOrWhiteSpace(p)) set.Add(p.Trim());
            }
            catch { /* ignore; fall through */ }

            // 2) BCL
            try
            {
                foreach (var p in System.IO.Ports.SerialPort.GetPortNames())
                    if (!string.IsNullOrWhiteSpace(p)) set.Add(p.Trim());
            }
            catch { /* ignore; fall through */ }

            // 3) Unix: explicit /dev scan for stability & completeness
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (var dev in EnumerateUnixSerialPorts())
                    set.Add(dev);
            }

            Ports.Clear();
            foreach (var p in set.OrderBy(PortOrder).ThenBy(p => p)) Ports.Add(p);

            // keep a configured-but-missing port visible so the editor shows it
            if (Selected is not null && !string.IsNullOrWhiteSpace(Selected.Port) && !Ports.Contains(Selected.Port))
                Ports.Add(Selected.Port);

            if (Ports.Count == 0)
            {
                // helpful Linux/macOS hints
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    Status = "No serial ports found. On Linux, check /dev/ttyACM* or /dev/ttyUSB*, and that your user is in the 'dialout' (or 'uucp') group. Re-login after adding.";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    Status = "No serial ports found. On macOS, check /dev/tty.* or /dev/cu.* and USB driver permissions.";
                else
                    Status = "No serial ports found.";
            }
            else
            {
                Status = $"Found {Ports.Count} port(s): {string.Join(", ", Ports)}";
            }
        }

        private static IEnumerable<string> EnumerateUnixSerialPorts()
        {
            // Prefer stable symlinks by-id (human readable, persist across reboots)
            foreach (var p in SafeGlob("/dev/serial/by-id/*"))
                yield return p;

            // Common USB CDC and FTDI-style
            foreach (var p in SafeGlob("/dev/ttyACM*")) yield return p;
            foreach (var p in SafeGlob("/dev/ttyUSB*")) yield return p;

            // On some systems, these can be relevant (hardware UARTs)
            foreach (var p in SafeGlob("/dev/ttyS*")) yield return p;

            // macOS style (if running under OS X)
            foreach (var p in SafeGlob("/dev/tty.*")) yield return p;
            foreach (var p in SafeGlob("/dev/cu.*")) yield return p;
        }

        private static IEnumerable<string> SafeGlob(string pattern)
        {
            string? dir = null;
            string? mask = null;
            IEnumerable<string> results = Array.Empty<string>();

            try
            {
                dir = Path.GetDirectoryName(pattern);
                mask = Path.GetFileName(pattern);

                if (!string.IsNullOrEmpty(dir) &&
                    !string.IsNullOrEmpty(mask) &&
                    Directory.Exists(dir))
                {
                    // Do all IO here
                    results = Directory.EnumerateFileSystemEntries(dir, mask);
                }
            }
            catch
            {
                // swallow – we'll just return empty
                results = Array.Empty<string>();
            }

            // Yield outside try/catch
            foreach (var path in results)
                yield return path;
        }


        private static int PortOrder(string name)
        {
            // Windows: COM<number> sorts numerically
            if (name.StartsWith("COM", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(name.AsSpan(3), out var n))
                return n;

            // Unix: prefer by-id (stable), then ACM/USB, then others
            if (name.StartsWith("/dev/serial/by-id/", StringComparison.Ordinal)) return 0;
            if (name.StartsWith("/dev/ttyACM", StringComparison.Ordinal)) return 1;
            if (name.StartsWith("/dev/ttyUSB", StringComparison.Ordinal)) return 2;
            if (name.StartsWith("/dev/ttyS", StringComparison.Ordinal)) return 3;
            if (name.StartsWith("/dev/tty.", StringComparison.Ordinal)) return 4; // macOS
            if (name.StartsWith("/dev/cu.", StringComparison.Ordinal)) return 5; // macOS

            return int.MaxValue;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        public void Close()
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
