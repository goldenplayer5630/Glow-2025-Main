using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Flower.App.Views;

public partial class SendCommandToFlowerView : UserControl
{
    public SendCommandToFlowerView()
    {
        InitializeComponent();
    }

    public SendCommandToFlowerView(object vm) : this()
    {
        DataContext = vm;
    }
}