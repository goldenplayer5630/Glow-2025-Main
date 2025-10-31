using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public interface IAppViewModel
    {
        // State
        ObservableCollection<string> Ports { get; }
        string? SelectedPort { get; set; }
        string StatusText { get; set; }
        string MonitorText { get; set; }

        // Commands
        ReactiveCommand<Unit, Unit> RefreshPortsCommand { get; }
        ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        ReactiveCommand<Unit, Unit> LoadShowCommand { get; }
        ReactiveCommand<Unit, Unit> PlayCommand { get; }
        ReactiveCommand<Unit, Unit> StopCommand { get; }
        ReactiveCommand<Unit, Unit> OpenShowCreatorCommand { get; }
    }
}
