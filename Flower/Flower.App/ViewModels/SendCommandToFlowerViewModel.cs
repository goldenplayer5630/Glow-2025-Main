// Flower.App.ViewModels/SendCommandToFlowerViewModel.cs
using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Models;
using Flower.Core.Records;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Text.Json;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public sealed class SendCommandToFlowerViewModel : ViewModelBase, ISendCommandToflowerViewModel
    {
        private readonly ICommandService _commandService;
        private readonly ICommandRegistry _registry;
        private readonly IUiLogService? _uiLog;
        private readonly CompositeDisposable _argSubs = new();

        private FlowerUnit? _target;
        private bool _isBusy;
        private bool _closeAfterSend;                    // NEW
        private CommandOption? _selectedCommand;
        private string _argsJson = "{}";

        public event EventHandler<string?>? CloseRequested;

        public string HeaderText { get; private set; } = "Send command to selected flower";
        public IReadOnlyList<CommandOption> CommandOptions { get; }

        public ObservableCollection<ArgEntry> ArgEntries { get; } = new();

        public bool CloseAfterSend                              // NEW (bind to a checkbox in XAML)
        {
            get => _closeAfterSend;
            set => this.RaiseAndSetIfChanged(ref _closeAfterSend, value);
        }

        public CommandOption? SelectedCommand
        {
            get => _selectedCommand;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedCommand, value);
                LoadArgsForSelected();
                this.RaisePropertyChanged(nameof(CanConfirm));
            }
        }

        public string ArgsJson
        {
            get => _argsJson;
            private set { this.RaiseAndSetIfChanged(ref _argsJson, value); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { this.RaiseAndSetIfChanged(ref _isBusy, value); this.RaisePropertyChanged(nameof(CanConfirm)); }
        }

        public bool CanConfirm =>
            !IsBusy &&
            _target is not null &&
            SelectedCommand is not null;

        public SendCommandToFlowerViewModel(
            ICommandService commandService,
            ICommandRegistry registry,
            IUiLogService? uiLog = null)             // NEW optional logger
        {
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _uiLog = uiLog;

            var all = _registry.AllCommands.ToList();
            CommandOptions = all
                .Select(c => new CommandOption(c.Id, c.DisplayName ?? c.Id))
                .OrderBy(c => c.Title)
                .ToList();

            SelectedCommand = CommandOptions.FirstOrDefault();

            ArgEntries.CollectionChanged += (_, __) => RebuildArgsJson();
        }

        public Task InitAsync(FlowerUnit target)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            HeaderText = $"Send command to flower #{_target.Id}";
            this.RaisePropertyChanged(nameof(HeaderText));
            this.RaisePropertyChanged(nameof(CanConfirm));
            return Task.CompletedTask;
        }

        public async Task ConfirmAsync()
        {
            if (!CanConfirm) return;
            IsBusy = true;

            var ts = DateTimeOffset.Now;
            var commandId = SelectedCommand!.Id;
            var args = BuildArgsDictionary();

            try
            {
                // No ConfigureAwait(false) – keep context friendly for UI signaling
                await _commandService.SendCommandAsync(commandId, _target!, args);

                _uiLog?.Info(
                    $"Sent {commandId} to flower #{_target!.Id} args={ArgsJson}");

                if (CloseAfterSend)
                    CloseRequested?.Invoke(this, commandId);  // window will close
                // else: keep window open for rapid subsequent sends
            }
            catch (Exception ex)
            {
                _uiLog?.Error(
                    $"Failed to sent {commandId} to flower #{_target?.Id} args={ArgsJson}",
                    ex);

                // keep dialog open to let user adjust + re-send
                // but still allow consumers to close if they want:
                // CloseRequested?.Invoke(this, null);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Cancel() => CloseRequested?.Invoke(this, null);

        // -------- internals --------
        private void LoadArgsForSelected()
        {
            _argSubs.Clear();
            ArgEntries.Clear();

            if (SelectedCommand is null) { RebuildArgsJson(); return; }

            var desc = _registry.GetById(SelectedCommand.Id);
            var map = desc.args ?? new Dictionary<string, object?>();

            foreach (var (name, objVal) in map)
            {
                double v = 0;
                try
                {
                    if (objVal is IConvertible conv) v = Convert.ToDouble(conv);
                }
                catch { v = 0; }

                var entry = new ArgEntry(name, v);
                ArgEntries.Add(entry);

                entry.WhenAnyValue(x => x.Value)
                    .Subscribe(_ => RebuildArgsJson())
                    .DisposeWith(_argSubs);
            }

            RebuildArgsJson();
        }

        private void RebuildArgsJson()
        {
            var dict = ArgEntries.ToDictionary(
                a => a.Name,
                a => NumberBox(a.Value)
            );
            ArgsJson = JsonSerializer.Serialize(dict);
        }

        private static object NumberBox(double d)
            => Math.Abs(d - Math.Round(d)) < 1e-9 ? (object)(int)Math.Round(d) : d;

        private IReadOnlyDictionary<string, object> BuildArgsDictionary()
            => ArgEntries.ToDictionary(a => a.Name, a => NumberBox(a.Value));
    }
}
