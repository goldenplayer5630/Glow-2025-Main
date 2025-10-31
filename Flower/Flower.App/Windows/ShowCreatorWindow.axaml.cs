using Avalonia.Controls;
using Flower.App.ViewModels;

namespace Flower.App.Windows;

public partial class ShowCreatorWindow : Window
{
    public ShowCreatorWindow()
    {
        InitializeComponent();
    }

    public ShowCreatorWindow(IShowCreatorViewModel vm) : this()
    {
        DataContext = vm;
    }
}
