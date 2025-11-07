using Flower.Core.Enums;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Models
{
    [JsonObject(MemberSerialization.OptOut)]
    public class FlowerUnit : ReactiveObject // ⬅️ inherit ReactiveObject
    {
        // Minimal typical fields
        private int _id;
        private FlowerCategory _category;
        private ConnectionStatus _connectionStatus;
        private FlowerStatus _flowerStatus;
        private int _currentBrightness;
        private int _priority;
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

        [JsonIgnore]
        public ConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
        }

        [JsonIgnore]
        public FlowerStatus FlowerStatus
        {
            get => _flowerStatus;
            set => this.RaiseAndSetIfChanged(ref _flowerStatus, value);
        }

        [JsonIgnore]
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

        public int Priority
        {
            get => _priority;
            set => this.RaiseAndSetIfChanged(ref _priority, value);
        }
    }
}
