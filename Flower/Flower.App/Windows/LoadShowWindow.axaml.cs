using Avalonia.Controls;
using Avalonia.Threading;
using Flower.App.ViewModels;
using Flower.Core.Models;

namespace Flower.App.Windows
{
    public partial class LoadShowWindow : Window
    {
        public LoadShowWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Closing += OnClosing;
            Closed += (_, __) => Detach();
        }

        private void OnDataContextChanged(object? s, System.EventArgs e)
        {
            Detach();
            if (DataContext is ILoadShowViewModel vm)
                Attach(vm);
        }

        private void Attach(ILoadShowViewModel vm)
        {
            vm.CloseRequested -= OnVmCloseRequested;
            vm.CloseRequested += OnVmCloseRequested;
        }

        private void Detach()
        {
            if (DataContext is ILoadShowViewModel vm)
                vm.CloseRequested -= OnVmCloseRequested;
        }

        private void OnVmCloseRequested(object? sender, ShowProject? project)
        {
            if (Dispatcher.UIThread.CheckAccess())
                Close(project);
            else
                Dispatcher.UIThread.Post(() => Close(project));
        }

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            // Allow closing. If you want to block while loading, add IsBusy to the interface and check here.
        }
    }
}
