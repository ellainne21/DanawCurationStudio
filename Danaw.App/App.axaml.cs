using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Danaw.App.ViewModels;
using Danaw.App.Views;
using Danaw.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Danaw.App;

public partial class App : Application
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;

    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;
            Services = Startup.BuildServices();
            ShowLogin();
        }

        base.OnFrameworkInitializationCompleted();
    }

    // The app always starts at the sign-in window, and returns to it on Sign out.
    private void ShowLogin()
    {
        var vm = new LoginViewModel(Services.GetRequiredService<IServiceScopeFactory>());
        var window = new LoginWindow { DataContext = vm };
        vm.SignedIn += ShowMain;

        SwitchTo(window);
        _ = vm.InitializeAsync();
    }

    private void ShowMain(Reviewer user)
    {
        var vm = new MainViewModel(Services.GetRequiredService<IServiceScopeFactory>(), user);
        var window = new MainWindow { DataContext = vm };
        vm.SignOutRequested += ShowLogin;

        SwitchTo(window);
        _ = vm.InitializeAsync();
    }

    // Opens the next window first, then closes the previous one, so the app never has zero windows.
    private void SwitchTo(Window next)
    {
        if (_desktop is null) return;

        var previous = _desktop.MainWindow;
        _desktop.MainWindow = next;

        if (previous is not null)
        {
            next.Show();
            previous.Close();
        }
    }
}