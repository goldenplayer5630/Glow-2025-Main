using Flower.Core.Abstractions.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public sealed class AssignBusViewModel : ViewModelBase, IAssignBusViewModel
    {
        private readonly List<string> _busIds;

        public IReadOnlyList<string> BusIds => _busIds;

        private string? _selectedBusId;
        public string? SelectedBusId
        {
            get => _selectedBusId;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedBusId, value);
                this.RaisePropertyChanged(nameof(CanConfirm));
            }
        }

        public string HeaderText { get; }

        public bool CanConfirm => !string.IsNullOrWhiteSpace(SelectedBusId);

        public AssignBusViewModel(List<string> busIds, int selectedCount = 0)
        {
            _busIds = busIds ?? new List<string>();
            _selectedBusId = _busIds.FirstOrDefault();

            HeaderText = selectedCount > 0
                ? $"Assign {selectedCount} flower(s) to which bus?"
                : "Assign selected flower(s) to which bus?";
        }
    }
}
