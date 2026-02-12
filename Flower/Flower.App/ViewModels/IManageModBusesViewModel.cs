using Flower.Core.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;

namespace Flower.App.ViewModels
{
    public interface IManageModBusesViewModel : IDisposable
    {
        // Data (filtered: Modbus only)
        ReadOnlyObservableCollection<BusConfig> Buses { get; }
        IReadOnlyList<string> BusIdOptions { get; }

        // Selection
        BusConfig? Selected { get; set; }

        // Editors for Add / Update
        string EditBusId { get; set; }
        string? EditHost { get; set; }   // ModbusHost
        int EditPort { get; set; }       // ModbusPort
        byte EditUnitId { get; set; }    // ModbusUnitId
        int EditTimeoutMs { get; set; }  // ModbusConnectTimeoutMs

        // UI Status
        string Status { get; set; }

        // Commands
        ReactiveCommand<Unit, Unit> SaveCommand { get; }
        ReactiveCommand<Unit, Unit> AddCommand { get; }
        ReactiveCommand<Unit, Unit> UpdateCommand { get; }
        ReactiveCommand<Unit, Unit> DeleteCommand { get; }
        ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        ReactiveCommand<Unit, Unit> DisconnectCommand { get; }
        ReactiveCommand<Unit, Unit> TestCommand { get; } // NEW
        ReactiveCommand<Unit, Unit> ConnectAllCommand { get; }
        ReactiveCommand<Unit, Unit> DisconnectAllCommand { get; }

        void Close();
    }
}
