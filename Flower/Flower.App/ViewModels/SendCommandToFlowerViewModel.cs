using Flower.Core.Abstractions.Commands;
using Flower.Core.Models;
using Flower.Core.Records;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public sealed class SendCommandToFlowerViewModel : ViewModelBase, ISendCommandToflowerViewModel
    {
        private readonly ICommandService _commandService;
        private readonly ICommandRegistry _registry;
        private readonly CompositeDisposable _argSubs = new();

        private FlowerUnit? _target;
        private bool _isBusy;
        private CommandOption? _selectedCommand;
        private string _argsJson = "{}";

        public event EventHandler<string?>? CloseRequested;

        public string HeaderText { get; private set; } = "Send command to selected flower";
        public IReadOnlyList<CommandOption> CommandOptions { get; }

        // Auto UI for arguments
        public ObservableCollection<ArgEntry> ArgEntries { get; } = new();

        public CommandOption? SelectedCommand
        {
            get => _selectedCommand;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedCommand, value);
                LoadArgsForSelected();          // <-- refresh numeric fields when command changes
                this.RaisePropertyChanged(nameof(CanConfirm));
            }
        }

        // Optional: still show JSON (kept in sync)
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
            SelectedCommand is not null; // numeric fields are always valid numbers

        public SendCommandToFlowerViewModel(ICommandService commandService, ICommandRegistry registry)
        {
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));

            var all = _registry.AllCommands.ToList();
            CommandOptions = all
                .Select(c => new CommandOption(c.Id, c.DisplayName ?? c.Id))
                .OrderBy(c => c.Title)
                .ToList();

            SelectedCommand = CommandOptions.FirstOrDefault();

            // keep JSON preview in sync when numbers change
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
            try
            {
                var args = BuildArgsDictionary(); // from ArgEntries
                var commandId = SelectedCommand!.Id;

                await _commandService.SendCommandAsync(commandId, _target!, args).ConfigureAwait(false);
                CloseRequested?.Invoke(this, commandId);
            }
            catch
            {
                CloseRequested?.Invoke(this, null);
            }
            finally { IsBusy = false; }
        }

        public void Cancel() => CloseRequested?.Invoke(this, null);

        // -------- internals --------
        private void LoadArgsForSelected()
        {
            // Clear previous arg subscriptions
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

                // ⬇ subscribe correctly (Changed is IObservable)
                entry.WhenAnyValue(x => x.Value)
                    .Subscribe(_ => RebuildArgsJson())
                    .DisposeWith(_argSubs);
            }

            // If the collection itself changes later, refresh JSON
            // (optional) – if you add/remove args dynamically:
            // ArgEntries.CollectionChanged += (_, __) => RebuildArgsJson();

            RebuildArgsJson();
        }

        private void OnArgEntryChanged(object? sender, EventArgs e) => RebuildArgsJson();

        private void RebuildArgsJson()
        {
            var dict = ArgEntries.ToDictionary(
                a => a.Name,
                a => NumberBox(a.Value) // int if whole, else double
            );
            ArgsJson = JsonSerializer.Serialize(dict);
        }

        private static object NumberBox(double d)
            => Math.Abs(d - Math.Round(d)) < 1e-9 ? (object)(int)Math.Round(d) : d;

        private IReadOnlyDictionary<string, object> BuildArgsDictionary()
            => ArgEntries.ToDictionary(a => a.Name, a => NumberBox(a.Value));


    }

}
