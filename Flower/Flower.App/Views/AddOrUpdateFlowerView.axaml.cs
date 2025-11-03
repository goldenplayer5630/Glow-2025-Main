// Flower.App/Views/AddFlowerView.axaml.cs
using Avalonia.Controls;
using Flower.App.ViewModels;
using Flower.App.Views;

namespace Flower.App.Views
{
    public partial class AddFlowerView : UserControl
    {
        public AddFlowerView()
        {
            InitializeComponent();
        }

        public AddFlowerView(IAddOrUpdateFlowerViewModel vm) : this()
        {
            DataContext = vm;
        }
    }
}