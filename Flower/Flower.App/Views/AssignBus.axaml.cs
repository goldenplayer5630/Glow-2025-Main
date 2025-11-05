using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Flower.App;

public partial class AssignBus : UserControl
{
    public AssignBus()
    {
        InitializeComponent();
    }

    public AssignBus(object vm) : this()
    {
        DataContext = vm;
    }
}