using Avalonia.Controls;
using Flower.App.ViewModels;

namespace Flower.App.Windows
{
    public partial class LoadFlowersWindow : Window
    {

        public LoadFlowersWindow()
        {
            InitializeComponent();
        }
        public LoadFlowersWindow(ILoadFlowerViewModel vm) : this()
        {
            DataContext = vm;
        }
    }
}
