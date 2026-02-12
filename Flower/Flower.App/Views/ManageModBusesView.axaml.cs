using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Flower.App.ViewModels;

namespace Flower.App.Windows;

public partial class ManageModBusesView : UserControl
{
    public ManageModBusesView()
    {
        InitializeComponent();
    }

    public ManageModBusesView(IAddOrUpdateFlowerViewModel vm) : this()
    {
        DataContext = vm;
    }
}