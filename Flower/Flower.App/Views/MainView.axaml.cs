using Avalonia.Controls;
using Flower.App.ViewModels;

namespace Flower.App.Views;

public partial class MainView : UserControl
{
    public MainView() => InitializeComponent();

    public MainView(IShowCreatorViewModel vm) : this()
    {
        DataContext = vm;
    }
}
