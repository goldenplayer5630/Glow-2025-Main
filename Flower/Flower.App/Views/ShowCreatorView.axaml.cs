// Flower.App/Views/ShowCreatorView.axaml.cs
using Avalonia.Controls;
using Flower.App.ViewModels;

namespace Flower.App.Views;

public partial class ShowCreatorView : UserControl
{
    // Previewer / design-time
    public ShowCreatorView()
    {
        InitializeComponent();
    }

    // Runtime via DI
    public ShowCreatorView(IShowCreatorViewModel vm) : this()
    {
        DataContext = vm;
    }
}
