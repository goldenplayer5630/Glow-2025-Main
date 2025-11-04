
using Avalonia.Threading;
using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Abstractions.Stores;
using Flower.Core.Models;
using Flower.Core.Records;
using Flower.Core.Services;
using ReactiveUI;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public interface IAppViewModel : IDisposable
    {
        // ========= Bindable State =========
        ObservableCollection<string> Ports { get; }

        object? CurrentPage { get; set; }

        string? SelectedPortBus0 { get; set; }
        string? SelectedPortBus1 { get; set; }

        string StatusText { get; set; }
        string MonitorText { get; set; }

        // ========= Flowers (data + selection) =========
        ReadOnlyObservableCollection<FlowerUnit> Flowers { get; }

        FlowerUnit? SelectedFlower { get; set; }
        string SelectedFlowerInfo { get; }

        // ========= Visibility Helpers: Single Flower =========
        bool CanConnect { get; }
        bool CanDisconnect { get; }
        bool CanReconnect { get; }
        bool DisabledConnect { get; }

        // ========= Visibility Helpers: Fleet =========
        bool HasFlowers { get; }
        bool CanConnectAll { get; }
        bool CanDisconnectAll { get; }
        bool DisabledConnectAll { get; }

        // ========= Commands =========
        ReactiveCommand<Unit, Unit> RefreshPortsCommand { get; }
        ReactiveCommand<Unit, Unit> ConnectBusesCommand { get; }
        ReactiveCommand<Unit, Unit> LoadShowCommand { get; }
        ReactiveCommand<Unit, Unit> PlayCommand { get; }
        ReactiveCommand<Unit, Unit> StopCommand { get; }

        ReactiveCommand<Unit, Unit> ConnectFlowerCommand { get; }
        ReactiveCommand<Unit, Unit> DisconnectFlowerCommand { get; }
        ReactiveCommand<Unit, Unit> ReconnectFlowerCommand { get; }

        ReactiveCommand<Unit, Unit> ConnectAllFlowersCommand { get; }
        ReactiveCommand<Unit, Unit> DisconnectAllFlowersCommand { get; }

        ReactiveCommand<Unit, Unit> AddFlowerCommand { get; }
        ReactiveCommand<Unit, Unit> UpdateFlowerCommand { get; }
        ReactiveCommand<Unit, Unit> DeleteSelectedFlowerCommand { get; }

        // ========= Dialog Interactions =========
        ReactiveCommand<Unit, Unit> OpenShowCreatorCommand { get; }
        Interaction<Unit, Unit> OpenShowCreatorInteraction { get; }

        Interaction<Unit, FlowerUnit?> AddFlowerInteraction { get; }
        Interaction<FlowerUnit, FlowerUnit?> UpdateFlowerInteraction { get; }
    }
}
