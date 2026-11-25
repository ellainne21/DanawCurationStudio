
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Danaw.Core.Abstractions;
using Danaw.Core.Entities;
using Danaw.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Danaw.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopes;

    public MainViewModel(IServiceScopeFactory scopes, Reviewer user)
    {
        _scopes = scopes;
        CurrentUser = user;
        Queue = new ContentQueueViewModel(scopes, user.Role);
        StatusMessage = $"Database: {Startup.DatabasePath}";
    }

    // The app listens to this and returns to the sign-in window.
    public event Action? SignOutRequested;

    // Who is signed in. Set once at sign-in; it cannot be changed from the screen.
    public Reviewer CurrentUser { get; }
    public ContentQueueViewModel Queue { get; }

    public bool IsResearcher => CurrentUser.Role == ReviewerRole.Researcher;
    public bool IsValidator => CurrentUser.Role == ReviewerRole.Validator;

    // Text for the user card in the sidebar and the page header.
    public string UserName => CurrentUser.FullName;
    public string RoleText => IsValidator ? "BCPCH Validator" : "Researcher";
    public string UserInitial => string.IsNullOrWhiteSpace(CurrentUser.FullName)
        ? "?"
        : CurrentUser.FullName.Trim()[..1].ToUpperInvariant();

    public string PageSubtitle => IsValidator
        ? "Verify sources and record validation decisions."
        : "Create, edit and submit content items with verified sources.";

    [ObservableProperty] private string _statusMessage = string.Empty;

    // The sample-data button is shown only in Debug builds, never in the published app.
#if DEBUG
    public bool ShowSampleButton => IsResearcher;
#else
    public bool ShowSampleButton => false;
#endif

    public Task InitializeAsync() => Queue.RefreshAsync();

    [RelayCommand]
    private void SignOut() => SignOutRequested?.Invoke();

    // Debug helper: creates three items through the real Workflow 1 service.
    [RelayCommand]
    private async Task AddSampleItemsAsync()
    {
        if (!IsResearcher) return;

        using var scope = _scopes.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IContentCurationService>();

        var sources = new[]
        {
            new SourceInput("BCPCH - Meranaw heritage profile, 2023", "Institutional", "BCPCH", "BCPCH 2023", 2023),
            new SourceInput("NCCA TALAPAMANA registry entry", "Institutional", "NCCA", "Entry #4471", null)
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
        if (!IsResearcher)
        {
            StatusMessage = "Only researchers can create or edit content items.";
            return null;
        }

        if (row is not null)
        {
            if (row.CreatedById != CurrentUser.ReviewerId)
            {
                StatusMessage = "You can only edit items that you created.";
                return null;
            }

            if (row.Status is not (ContentStatus.Draft or ContentStatus.NeedsRevision))
            {
                StatusMessage = "Only Draft or Needs Revision items can be edited.";
                return null;
            }
        }

        var editor = new ItemEditorViewModel(_scopes, CurrentUser, row?.Id);
        await editor.LoadAsync();
        return editor;
    }

    // Checks who may review, then prepares the validation screen. Returns null if not allowed.
    public async Task<ValidationViewModel?> PrepareValidationAsync(ContentItemRow? row)
    {
        if (!IsValidator)
        {
            StatusMessage = "Only the BCPCH validator can review items.";
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