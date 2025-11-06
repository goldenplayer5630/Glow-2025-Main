using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Flower.App.Windows;            // MainWindow
using Flower.App.Views;              // MainView (for single-view lifetime, if you use it)
using Flower.Core.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Flower.App
{
    public partial class App : Application
    {
        private readonly IHost? _host;

        // Runtime ctor (DI host provided)
        public App(IHost host) => _host = host;

        // Designer/previewer ctor
        public App() { }

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Defer window creation until Startup so we can await async work first.
                desktop.Startup += async (_, __) => await StartDesktopAsync(desktop);
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime single)
            {
                // Same idea for single-view (mobile/embedded): run startup, then set MainView.
                _ = StartSingleViewAsync(single);
            }

            base.OnFrameworkInitializationCompleted();
        }

        // ===== Desktop path =====
        private async Task StartDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                if (_host is not null)
                {
                    // 1) Run your startup tasks (loads flowers/buses etc.)
                    var startup = _host.Services.GetRequiredService<IStartupTask>();
                    await startup.ExecuteAsync(); // stay on UI thread – no ConfigureAwait(false)

                    // 2) Create + show the main window on the UI thread
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var window = ActivatorUtilities.CreateInstance<MainWindow>(_host.Services);
                        desktop.MainWindow = window;
                        window.Show();
                    });
                }
                else
                {
                    // Design/preview fallback
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var window = new MainWindow();
                        desktop.MainWindow = window;
                        window.Show();
                    });
                }
            }
            catch (Exception ex)
            {
                // Show a simple error window so you see what failed.
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var error = new MainWindow
                    {
                        Content = new TextBox
                        {
                            IsReadOnly = true,
                            AcceptsReturn = true,
                            Text = "Startup failed:\n\n" + ex
                        }
                    };
                    desktop.MainWindow = error;
                    error.Show();
                });
            }
        }

        // ===== Single-view path (optional; safe default) =====
        private async Task StartSingleViewAsync(ISingleViewApplicationLifetime single)
        {
            try
            {
                if (_host is not null)
                {
                    var startup = _host.Services.GetRequiredService<IStartupTask>();
                    await startup.ExecuteAsync();

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Prefer a UserControl for single-view lifetimes.
                        var mainView = ActivatorUtilities.CreateInstance<MainView>(_host.Services);
                        single.MainView = mainView;
                    });
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        single.MainView = new MainView();
                    });
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    single.MainView = new TextBox
                    {
                        IsReadOnly = true,
                        AcceptsReturn = true,
                        Text = "Startup failed:\n\n" + ex
                    };
                });
            }
        }
    }
}
