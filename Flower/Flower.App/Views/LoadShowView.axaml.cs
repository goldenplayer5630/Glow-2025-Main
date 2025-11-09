using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Flower.App.Views
{
    public partial class LoadShowView : UserControl
    {
        public LoadShowView() => InitializeComponent();

        public LoadShowView(object vm) : this() {
            DataContext = vm;
        }
    }
}