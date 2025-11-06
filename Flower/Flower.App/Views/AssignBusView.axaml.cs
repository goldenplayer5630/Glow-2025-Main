using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Flower.App.Views;

public partial class AssignBusView : UserControl
{
    public AssignBusView()
    {
        InitializeComponent();
    }

    public AssignBusView(object vm) : this()
    {
        DataContext = vm;
    }
}