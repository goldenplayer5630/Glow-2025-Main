using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Flower.App;

public partial class App : Application
{
    private readonly IHost? _host;

    public App(IHost host) => _host = host;  // runtime
    public App() { }                          // previewer

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_host is not null)
                desktop.MainWindow = ActivatorUtilities.CreateInstance<Windows.MainWindow>(_host.Services);
            //else
            //    desktop.MainWindow = new Windows.MainWindow(); // previewer fallback
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            if (_host is not null)
                single.MainView = ActivatorUtilities.CreateInstance<Windows.MainWindow>(_host.Services);
            //else
            //    single.MainView = new Windows.MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
