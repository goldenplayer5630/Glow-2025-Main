using Avalonia.Controls;
using Avalonia.Interactivity;
using Flower.App.ViewModels;
using System;

namespace Flower.App.Windows
{
    public partial class ManageSerialBusesWindow : Window
    {
        public ManageSerialBusesWindow() => InitializeComponent();

        public ManageSerialBusesWindow(IManageBusesViewModel vm) : this()
        {
            DataContext = vm;

            var closeBtn = this.FindControl<Button>("CloseButton");

            if (closeBtn is not null) closeBtn.Click += OnCloseClicked;
        }


        private void OnCloseClicked(object? sender, RoutedEventArgs e)
        {
            if (DataContext is IManageBusesViewModel vm)
                vm.Close(); // keeps VM semantics

            Close(null);
        }
    }
}

