using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Models
{
    public class FlowerUnit
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public FlowerCategory Category { get; set; }

        public string BusId { get; set; } = "None";

        public ConnectionStatus ConnectionStatus { get; set; }
        public FlowerStatus FlowerStatus { get; set; }
        public int CurrentBrightness { get; set; }

        private bool _assignSelected;
        [System.Text.Json.Serialization.JsonIgnore]
        public bool AssignSelected
        {
            get => _assignSelected;
            set
            {
                if (_assignSelected == value) return;
                _assignSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AssignSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
