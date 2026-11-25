using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Danaw.Core.Abstractions;
using Danaw.Core.Entities;
using Danaw.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Danaw.App.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopes;

    public LoginViewModel(IServiceScopeFactory scopes) => _scopes = scopes;

    // The app listens to this and opens the main window for the signed-in user.
    public event Action<Reviewer>? SignedIn;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _databaseMessage = "Starting...";

    // true = the password is shown as plain text (the eye icon is toggled on)
    [ObservableProperty] private bool _showPassword;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSignIn))]
    private bool _databaseReady;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSignIn))]
    private bool _isBusy;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool CanSignIn => DatabaseReady && !IsBusy;

    // Creates or upgrades the database before anyone can sign in.
    public async Task InitializeAsync()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Startup.DatabasePath)!);

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DanawDbContext>();
            await db.Database.MigrateAsync(); // creates or upgrades the database, never wipes data

            DatabaseReady = true;
            DatabaseMessage = $"Database ready: {Startup.DatabasePath}";
        }
        catch (Exception ex)
        {
            DatabaseReady = false;
            DatabaseMessage = "The database could not be opened, so sign-in is unavailable. "
                            + $"Check that this location is available and writable: {Startup.DatabasePath}. "
                            + $"Details: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (!CanSignIn) return;
        ErrorMessage = null;
        IsBusy = true;

        try
        {
            var username = Username;
            var password = Password;

            // Password checking is deliberately slow, so it runs off the UI thread.
            var result = await Task.Run(async () =>
            {
                using var scope = _scopes.CreateScope();
                var auth = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
                return await auth.SignInAsync(username, password);
            });

            Password = string.Empty; // never keep the password around after use
            ShowPassword = false;    // and hide it again, so the next user starts hidden

            if (!result.Success)
            {
                ErrorMessage = result.Errors[0];
                return;
            }

            Username = string.Empty;
            SignedIn?.Invoke(result.Value!);
        }
        catch (Exception)
        {
            Password = string.Empty;
            ShowPassword = false;
            ErrorMessage = "Sign-in could not be completed because of a database problem. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}