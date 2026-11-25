using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Danaw.Core.Abstractions;
using Danaw.Core.Entities;
using Danaw.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Danaw.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopes;

    public MainViewModel(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
    }

    public ObservableCollection<Reviewer> Reviewers { get; } = new();

    [ObservableProperty] private Reviewer? _currentUser;
    [ObservableProperty] private string _statusMessage = "Starting...";
    [ObservableProperty] private bool _hasError;

    public async Task InitializeAsync()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Startup.DatabasePath)!);

            // A new scope gives this operation its own fresh DbContext.
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DanawDbContext>();
            await db.Database.MigrateAsync(); // creates or upgrades the database, never wipes data

            var reviewers = scope.ServiceProvider.GetRequiredService<IReviewerRepository>();
            foreach (var r in await reviewers.ListActiveAsync())
                Reviewers.Add(r);

            CurrentUser = Reviewers.Count > 0 ? Reviewers[0] : null;
            StatusMessage = $"Ready. Database: {Startup.DatabasePath}";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = "The database could not be opened, so the application cannot continue. "
                          + $"Check that this location is available and writable: {Startup.DatabasePath}. "
                          + $"Details: {ex.Message}";
        }
    }
}