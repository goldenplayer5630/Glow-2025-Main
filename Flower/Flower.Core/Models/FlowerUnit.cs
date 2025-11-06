using Flower.Core.Enums;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Models
{
    public class FlowerUnit : ReactiveObject // ⬅️ inherit ReactiveObject
    {
        // Minimal typical fields
        private int _id;
        private FlowerCategory _category;
        private ConnectionStatus _connectionStatus;
        private FlowerStatus _flowerStatus;
        private int _currentBrightness;
        private string? _busId;

        // This is the one that was throwing:
        private bool _assignSelected;

        public int Id
        {
            get => _id;
            set => this.RaiseAndSetIfChanged(ref _id, value);
        }

        public FlowerCategory Category
        {
            get => _category;
            set => this.RaiseAndSetIfChanged(ref _category, value);
        }

        public ConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
        }

        public FlowerStatus FlowerStatus
        {
            get => _flowerStatus;
            set => this.RaiseAndSetIfChanged(ref _flowerStatus, value);
        }

        public int CurrentBrightness
        {
            get => _currentBrightness;
            set => this.RaiseAndSetIfChanged(ref _currentBrightness, value);
        }

        public string? BusId
        {
            get => _busId;
            set => this.RaiseAndSetIfChanged(ref _busId, value);
        }

        public bool AssignSelected
        {
            get => _assignSelected;
            set => this.RaiseAndSetIfChanged(ref _assignSelected, value);
        }
    }
}
