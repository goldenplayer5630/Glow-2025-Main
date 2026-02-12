using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Flower.App.ViewModels;

namespace Flower.App.Views;

public partial class ManageModBusesView : UserControl
{
    public ManageModBusesView()
    {
        InitializeComponent();
    }

    public ManageModBusesView(object vm) : this()
    {
        DataContext = vm;
    }
}