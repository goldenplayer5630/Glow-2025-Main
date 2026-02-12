using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Flower.App.ViewModels;

namespace Flower.App.Views;

public partial class ManageSerialBusesView : UserControl
{
    public ManageSerialBusesView()
    {
        InitializeComponent();
    }

    public ManageSerialBusesView(IAddOrUpdateFlowerViewModel vm) : this()
    {
        DataContext = vm;
    }
}