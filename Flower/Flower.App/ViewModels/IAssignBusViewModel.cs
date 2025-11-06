using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public interface IAssignBusViewModel
    {
        public IReadOnlyList<string> BusIds { get; }
        public string? SelectedBusId { get; set; }

        public string HeaderText { get; }

        public bool CanConfirm => !string.IsNullOrWhiteSpace(SelectedBusId);
    }
}
