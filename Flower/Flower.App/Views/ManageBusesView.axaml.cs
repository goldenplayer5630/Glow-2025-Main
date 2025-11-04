using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Flower.App.ViewModels;

namespace Flower.App.Views;

public partial class ManageBusesView : UserControl
{
    public ManageBusesView()
    {
        InitializeComponent();
    }

    public ManageBusesView(IAddOrUpdateFlowerViewModel vm) : this()
    {
        DataContext = vm;
    }
}