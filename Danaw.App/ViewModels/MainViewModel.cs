using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Danaw.Core.Abstractions;
using Danaw.Core.Entities;
using Danaw.Core.Models;
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
        Queue = new ContentQueueViewModel(scopes);
    }

    public ContentQueueViewModel Queue { get; }
    public ObservableCollection<Reviewer> Reviewers { get; } = new();

    [ObservableProperty] private Reviewer? _currentUser;
    [ObservableProperty] private string _statusMessage = "Starting...";
    [ObservableProperty] private bool _hasError;

    // The sample-data button is shown only in Debug builds, never in the published app.
#if DEBUG
    public bool IsDebugBuild => true;
#else
    public bool IsDebugBuild => false;
#endif

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
            return;
        }

        await Queue.RefreshAsync();
    }

    // Debug helper: creates three items through the real Workflow 1 service.
    [RelayCommand]
    private async Task AddSampleItemsAsync()
    {
        if (CurrentUser is null || CurrentUser.Role != ReviewerRole.Researcher)
        {
            StatusMessage = "Select a researcher in the dropdown first, then add sample items.";
            return;
        }

        using var scope = _scopes.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IContentCurationService>();

        var sources = new[]
        {
            new SourceInput("BCPCH - Meranaw heritage profile, 2023", "Report", "BCPCH", "BCPCH 2023", 2023),
            new SourceInput("NCCA TALAPAMANA registry entry", "Registry", "NCCA", "Entry #4471", null)
        };

        var samples = new (string Title, int CategoryId, bool Submit)[]
        {
            ("Okir Motifs", 1, true),
            ("Inaul Weaving", 2, true),
            ("Darangen Epic", 3, false)   // saved as a Draft, no sources
        };

        var created = 0;
        foreach (var (title, categoryId, submit) in samples)
        {
            var input = new ContentItemInput(
                null, categoryId, title, "Sample", "Sample description for testing.",
                submit ? sources : Array.Empty<SourceInput>());

            var result = submit
                ? await service.SubmitAsync(CurrentUser.ReviewerId, input)
                : await service.SaveDraftAsync(CurrentUser.ReviewerId, input);

            if (result.Success) created++;
        }

        StatusMessage = created == samples.Length
            ? $"Added {created} sample items."
            : $"Added {created} of {samples.Length} sample items (the others already exist).";

        await Queue.RefreshAsync();
    }

    // Checks who may open the editor, then prepares it. Returns null if not allowed.
    public async Task<ItemEditorViewModel?> PrepareEditorAsync(ContentItemRow? row)
    {
        if (CurrentUser is null || CurrentUser.Role != ReviewerRole.Researcher)
        {
            StatusMessage = "Select a researcher in the dropdown to create or edit content items.";
            return null;
        }

        if (row is not null && row.Status is not (ContentStatus.Draft or ContentStatus.NeedsRevision))
        {
            StatusMessage = "Only Draft or Needs Revision items can be edited.";
            return null;
        }

        var editor = new ItemEditorViewModel(_scopes, CurrentUser, row?.Id);
        await editor.LoadAsync();
        return editor;
    }

    // Checks who may review, then prepares the validation screen. Returns null if not allowed.
    public async Task<ValidationViewModel?> PrepareValidationAsync(ContentItemRow? row)
    {
        if (CurrentUser is null || CurrentUser.Role != ReviewerRole.Validator)
        {
            StatusMessage = "Select the BCPCH validator in the dropdown to review items.";
            return null;
        }

        if (row is null)
        {
            StatusMessage = "Select an item in the list first.";
            return null;
        }

        if (row.Status == ContentStatus.Draft)
        {
            StatusMessage = "Drafts have not been submitted yet, so there is nothing to review.";
            return null;
        }

        var vm = new ValidationViewModel(_scopes, CurrentUser, row.Id);
        await vm.LoadAsync();
        return vm;
    }
}