using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Danaw.App.ViewModels;
using Danaw.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Danaw.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Services = Startup.BuildServices();

            var vm = new MainViewModel(Services.GetRequiredService<IServiceScopeFactory>());
            desktop.MainWindow = new MainWindow { DataContext = vm };

            // Loads data after the window opens; errors are shown in the status line.
            _ = vm.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}