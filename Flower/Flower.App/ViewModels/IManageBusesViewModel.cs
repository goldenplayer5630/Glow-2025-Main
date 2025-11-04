using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Models;
using ReactiveUI;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public interface IManageBusesViewModel : IDisposable
    {
        // Data
        ReadOnlyObservableCollection<BusConfig> Buses { get; }
        IReadOnlyList<string> BusIdOptions { get; }
        ObservableCollection<string> Ports { get; }

        // Selection
        BusConfig? Selected { get; set; }

        // Editors for Add / Update
        string EditBusId { get; set; }
        string? EditPort { get; set; }
        int EditBaud { get; set; }

        // UI Status
        string Status { get; set; }

        // Commands
        ReactiveCommand<Unit, Unit> RefreshPortsCommand { get; }
        ReactiveCommand<Unit, Unit> LoadCommand { get; }
        ReactiveCommand<Unit, Unit> SaveCommand { get; }
        ReactiveCommand<Unit, Unit> AddCommand { get; }
        ReactiveCommand<Unit, Unit> UpdateCommand { get; }
        ReactiveCommand<Unit, Unit> DeleteCommand { get; }
        ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        ReactiveCommand<Unit, Unit> DisconnectCommand { get; }
        ReactiveCommand<Unit, Unit> ConnectAllCommand { get; }
        ReactiveCommand<Unit, Unit> DisconnectAllCommand { get; }

        Task ConfirmAsync();
        void Cancel();
    }
}
