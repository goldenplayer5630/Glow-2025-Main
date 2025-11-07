using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Flower.App.Views;

public partial class LoadFlowersView : UserControl
{
    public LoadFlowersView()
    {
        InitializeComponent();
    }

    public LoadFlowersView(object vm) : this()
    {
        DataContext = vm;
    }
}